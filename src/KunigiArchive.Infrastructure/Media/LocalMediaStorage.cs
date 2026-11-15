using System.Security.Cryptography;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFtoImage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;

namespace KunigiArchive.Infrastructure.Media;

internal sealed class LocalMediaStorage : IMediaStorage
{
    private const string WebPrefix = "media/";
    private const string FileNameAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    // Stored images are shrunk to fit this box: plenty for viewing full screen, a fraction of a phone photo's size.
    private const int MaxDimension = 2048;
    private const int ThumbnailMaxWidth = 400;

    // Checked before decoding, so a small file that claims enormous dimensions can't exhaust memory.
    private const long MaxPixels = 100_000_000;

    private static readonly IReadOnlySet<IImageFormat> AllowedImageFormats = new HashSet<IImageFormat>
    {
        JpegFormat.Instance, PngFormat.Instance, GifFormat.Instance, WebpFormat.Instance
    };

    // Lossy WebP is far smaller for photos; lossless wins for graphics, logos and text (PDF pages).
    private static readonly WebpEncoder LossyWebp = new WebpEncoder
    {
        FileFormat = WebpFileFormatType.Lossy,
        Quality = 80
    };

    private static readonly WebpEncoder LosslessWebp = new WebpEncoder
    {
        FileFormat = WebpFileFormatType.Lossless
    };

    private readonly string _root;
    private readonly ILogger<LocalMediaStorage> _logger;

    public LocalMediaStorage(IOptions<MediaStorageOptions> options, ILogger<LocalMediaStorage> logger)
    {
        _root = options.Value.RootPath;
        _logger = logger;
    }

    public async Task<StoredMedia> SaveAsync(string directory, MediaUpload upload, CancellationToken cancellationToken)
    {
        string directoryPath = ResolveInsideRoot(directory);
        Directory.CreateDirectory(directoryPath);

        await using MemoryStream buffer = Path.GetExtension(upload.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            ? RenderPdfFirstPage(await BufferAsync(upload, cancellationToken), upload.FileName)
            : await BufferAsync(upload, cancellationToken);

        ImageInfo info = await IdentifyAsync(buffer, upload.FileName, cancellationToken);

        string id = RandomNumberGenerator.GetString(FileNameAlphabet, 11);
        string fileName = info.Metadata.DecodedImageFormat == GifFormat.Instance
            ? await SaveGifAsync(buffer, directoryPath, id, cancellationToken)
            : await SaveAsWebpAsync(buffer, info, directoryPath, id, upload.FileName, cancellationToken);

        return new StoredMedia($"{WebPrefix}{directory.Trim('/')}/{fileName}", fileName);
    }

    public void Delete(string path)
    {
        string relative = path.StartsWith(WebPrefix, StringComparison.Ordinal) ? path[WebPrefix.Length..] : path;

        try
        {
            string fullPath = ResolveInsideRoot(relative);
            File.Delete(fullPath);
            File.Delete(ThumbnailPath(fullPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogError(exception, "Failed to delete media file {Path}", path);
        }
    }

    public void DeleteDirectory(string directory)
    {
        try
        {
            string fullPath = ResolveInsideRoot(directory);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogError(exception, "Failed to delete media directory {Directory}", directory);
        }
    }

    // Only the image header is read here, which also rejects anything that isn't really one of the allowed formats
    // (a renamed HTML or script file can never end up being served from /media).
    private static async Task<ImageInfo> IdentifyAsync(MemoryStream buffer, string fileName, CancellationToken cancellationToken)
    {
        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(buffer, cancellationToken);
        }
        catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new InvalidRequestException($"Το αρχείο '{fileName}' δεν είναι έγκυρη εικόνα.");
        }

        IImageFormat? format = info.Metadata.DecodedImageFormat;
        if (format is null || !AllowedImageFormats.Contains(format))
        {
            throw new InvalidRequestException($"Η μορφή του αρχείου '{fileName}' δεν υποστηρίζεται.");
        }

        if ((long)info.Width * info.Height > MaxPixels)
        {
            throw new InvalidRequestException($"Η εικόνα '{fileName}' έχει υπερβολικά μεγάλες διαστάσεις.");
        }

        buffer.Position = 0;
        return info;
    }

    // Upright, shrunk, without camera metadata (it can include the GPS location), re-encoded as WebP.
    private async Task<string> SaveAsWebpAsync(
        MemoryStream buffer,
        ImageInfo info,
        string directoryPath,
        string id,
        string originalName,
        CancellationToken cancellationToken)
    {
        bool isPhoto = info.Metadata.DecodedImageFormat == JpegFormat.Instance;

        // TargetSize lets the JPEG decoder scale down while decoding, so a large photo never sits in memory at full size.
        // Only set for images that are too big: other decoders resize to it, enlarging smaller images too.
        DecoderOptions decoderOptions = info.Width > MaxDimension || info.Height > MaxDimension
            ? new DecoderOptions { TargetSize = new Size(MaxDimension, MaxDimension) }
            : new DecoderOptions();

        Image image;
        try
        {
            image = await Image.LoadAsync(decoderOptions, buffer, cancellationToken);
        }
        catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new InvalidRequestException($"Το αρχείο '{originalName}' δεν είναι έγκυρη εικόνα.");
        }

        using (image)
        {
            image.Mutate(x => x.AutoOrient());

            // ResizeMode.Max also enlarges smaller images, so only shrink.
            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(MaxDimension, MaxDimension) }));
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;

            string fileName = $"{id}.webp";
            string fullPath = Path.Combine(directoryPath, fileName);
            await SaveSmallestAsync(image, fullPath, isPhoto, cancellationToken);

            try
            {
                if (image.Width > ThumbnailMaxWidth)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(ThumbnailMaxWidth, 0) }));
                }

                await SaveSmallestAsync(image, ThumbnailPath(fullPath), isPhoto, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The upload still counts; pages fall back to the full image when the thumbnail is missing.
                _logger.LogWarning(exception, "Failed to create a thumbnail for {Path}", fullPath);
            }

            return fileName;
        }
    }

    // A JPEG is a photo, so lossy it is. Anything else may be a photo or a graphic: try both, keep the smaller.
    private static async Task SaveSmallestAsync(Image image, string fullPath, bool isPhoto, CancellationToken cancellationToken)
    {
        await using MemoryStream lossy = new MemoryStream();
        await image.SaveAsync(lossy, LossyWebp, cancellationToken);

        MemoryStream smallest = lossy;
        await using MemoryStream lossless = new MemoryStream();
        if (!isPhoto)
        {
            await image.SaveAsync(lossless, LosslessWebp, cancellationToken);
            if (lossless.Length < lossy.Length)
            {
                smallest = lossless;
            }
        }

        smallest.Position = 0;
        await using FileStream output = new FileStream(fullPath, FileMode.CreateNew);
        await smallest.CopyToAsync(output, cancellationToken);
    }

    // GIFs are kept byte for byte: re-encoding would lose or bloat animations.
    private async Task<string> SaveGifAsync(MemoryStream buffer, string directoryPath, string id, CancellationToken cancellationToken)
    {
        string fileName = $"{id}.gif";
        string fullPath = Path.Combine(directoryPath, fileName);

        await using (FileStream output = new FileStream(fullPath, FileMode.CreateNew))
        {
            await buffer.CopyToAsync(output, cancellationToken);
        }

        try
        {
            buffer.Position = 0;
            using Image image = await Image.LoadAsync(buffer, cancellationToken);
            if (image.Width > ThumbnailMaxWidth)
            {
                image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(ThumbnailMaxWidth, 0) }));
            }

            await image.SaveAsGifAsync(ThumbnailPath(fullPath), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Failed to create a thumbnail for {Path}", fullPath);
        }

        return fileName;
    }

    // The first page becomes an image and then goes through the same shrinking and encoding as any upload.
    private static MemoryStream RenderPdfFirstPage(MemoryStream pdf, string fileName)
    {
        using (pdf)
        {
            byte[] header = new byte[5];
            if (pdf.Read(header) != header.Length || !"%PDF-"u8.SequenceEqual(header))
            {
                throw new InvalidRequestException($"Το αρχείο '{fileName}' δεν είναι έγκυρο PDF.");
            }

            pdf.Position = 0;
            try
            {
                using SKBitmap firstPage = Conversion.ToImage(pdf, page: 0);
                MemoryStream png = new MemoryStream();
                firstPage.Encode(png, SKEncodedImageFormat.Png, quality: 100);
                png.Position = 0;
                return png;
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not IOException)
            {
                throw new InvalidRequestException($"Το PDF '{fileName}' δεν μπόρεσε να μετατραπεί σε εικόνα.");
            }
        }
    }

    private static async Task<MemoryStream> BufferAsync(MediaUpload upload, CancellationToken cancellationToken)
    {
        MemoryStream buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public MediaUsage GetTotalUsage()
    {
        return Measure(new DirectoryInfo(Path.GetFullPath(_root)));
    }

    public IReadOnlyDictionary<string, MediaUsage> GetUsageOfSubfolders(string directory)
    {
        DirectoryInfo parent = new DirectoryInfo(ResolveInsideRoot(directory));
        if (!parent.Exists)
        {
            return new Dictionary<string, MediaUsage>();
        }

        return parent.EnumerateDirectories().ToDictionary(folder => folder.Name, Measure, StringComparer.OrdinalIgnoreCase);
    }

    private static MediaUsage Measure(DirectoryInfo directory)
    {
        if (!directory.Exists)
        {
            return MediaUsage.None;
        }

        int files = 0;
        long bytes = 0;
        foreach (FileInfo file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            bytes += file.Length;
            if (!file.Name.StartsWith(MediaRules.ThumbnailPrefix, StringComparison.Ordinal))
            {
                files++;
            }
        }

        return new MediaUsage(files, bytes);
    }

    private static string ThumbnailPath(string fullPath)
    {
        return Path.Combine(Path.GetDirectoryName(fullPath)!, MediaRules.ThumbnailPrefix + Path.GetFileName(fullPath));
    }

    private string ResolveInsideRoot(string relativePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        string root = Path.GetFullPath(_root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Media path '{relativePath}' resolves outside the media root.");
        }

        return fullPath;
    }
}
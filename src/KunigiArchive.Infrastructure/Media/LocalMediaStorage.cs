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
    private const int ThumbnailMaxWidth = 400;
    private const string FileNameAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly IReadOnlySet<IImageFormat> AllowedImageFormats = new HashSet<IImageFormat>
    {
        JpegFormat.Instance, PngFormat.Instance, GifFormat.Instance, WebpFormat.Instance
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

        string id = RandomNumberGenerator.GetString(FileNameAlphabet, 11);
        string extension = Path.GetExtension(upload.FileName).ToLowerInvariant();

        string fileName;
        if (extension == ".pdf")
        {
            fileName = $"{id}.png";
            await SavePdfAsPngAsync(upload, Path.Combine(directoryPath, fileName), cancellationToken);
        }
        else
        {
            fileName = $"{id}{extension}";
            await SaveImageAsync(upload, Path.Combine(directoryPath, fileName), cancellationToken);
        }

        await TryCreateThumbnailAsync(Path.Combine(directoryPath, fileName), cancellationToken);

        return new StoredMedia($"{WebPrefix}{directory.Trim('/')}/{fileName}", fileName);
    }

    public void Delete(string path)
    {
        string relative = path.StartsWith(WebPrefix, StringComparison.Ordinal) ? path[WebPrefix.Length..] : path;

        try
        {
            string fullPath = ResolveInsideRoot(relative);
            File.Delete(fullPath);
            File.Delete(Path.Combine(Path.GetDirectoryName(fullPath)!, MediaRules.ThumbnailPrefix + Path.GetFileName(fullPath)));
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

    // The original bytes are kept; ImageSharp only checks that they really are one of the allowed formats,
    // so a renamed HTML or script file can never end up being served from /media.
    private static async Task SaveImageAsync(MediaUpload upload, string fullPath, CancellationToken cancellationToken)
    {
        await using MemoryStream buffer = await BufferAsync(upload, cancellationToken);

        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(buffer, cancellationToken);
        }
        catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException)
        {
            throw new InvalidRequestException($"Το αρχείο '{upload.FileName}' δεν είναι έγκυρη εικόνα.");
        }

        if (!AllowedImageFormats.Contains(info.Metadata.DecodedImageFormat!))
        {
            throw new InvalidRequestException($"Η μορφή του αρχείου '{upload.FileName}' δεν υποστηρίζεται.");
        }

        buffer.Position = 0;
        await using FileStream output = new FileStream(fullPath, FileMode.CreateNew);
        await buffer.CopyToAsync(output, cancellationToken);
    }

    private static async Task SavePdfAsPngAsync(MediaUpload upload, string fullPath, CancellationToken cancellationToken)
    {
        await using MemoryStream buffer = await BufferAsync(upload, cancellationToken);

        byte[] header = new byte[5];
        if (buffer.Read(header) != header.Length || !"%PDF-"u8.SequenceEqual(header))
        {
            throw new InvalidRequestException($"Το αρχείο '{upload.FileName}' δεν είναι έγκυρο PDF.");
        }

        buffer.Position = 0;
        try
        {
            using SKBitmap firstPage = Conversion.ToImage(buffer, page: 0);
            await using FileStream output = new FileStream(fullPath, FileMode.CreateNew);
            firstPage.Encode(output, SKEncodedImageFormat.Png, quality: 100);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not IOException)
        {
            throw new InvalidRequestException($"Το PDF '{upload.FileName}' δεν μπόρεσε να μετατραπεί σε εικόνα.");
        }
    }

    private async Task TryCreateThumbnailAsync(string fullPath, CancellationToken cancellationToken)
    {
        string thumbnailPath = Path.Combine(Path.GetDirectoryName(fullPath)!, MediaRules.ThumbnailPrefix + Path.GetFileName(fullPath));
        try
        {
            using Image image = await Image.LoadAsync(fullPath, cancellationToken);
            if (image.Width > ThumbnailMaxWidth)
            {
                image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(ThumbnailMaxWidth, 0) }));
            }

            await image.SaveAsync(thumbnailPath, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The upload still counts; pages fall back to the full image when the thumbnail is missing.
            _logger.LogWarning(exception, "Failed to create a thumbnail for {Path}", fullPath);
        }
    }

    private static async Task<MemoryStream> BufferAsync(MediaUpload upload, CancellationToken cancellationToken)
    {
        MemoryStream buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
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
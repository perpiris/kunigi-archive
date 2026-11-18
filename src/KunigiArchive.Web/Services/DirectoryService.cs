using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.Utilities;
using KunigiArchive.Web.ViewModels.Admin;
using PDFtoImage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;

namespace KunigiArchive.Web.Services;

public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;
    private readonly string _basePath;

    public DirectoryService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DirectoryService> logger)
    {
        _logger = logger;
        _basePath = MediaStorage.ResolveRoot(configuration, environment);
    }

    public ServiceResponse CreateDirectory(string directoryPath)
    {
        string platformSpecificSubfolder = directoryPath.Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.Combine(_basePath, platformSpecificSubfolder);

        try
        {
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return ServiceResponse.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory at: {Path}", fullPath);
            return ServiceResponse.Failure($"Αποτυχία δημιουργίας φακέλου: {fullPath}");
        }
    }

    public ServiceResponse DeleteDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            _logger.LogWarning("Attempted to delete a directory with an empty path.");
            return ServiceResponse.Failure("Η διαδρομή του φακέλου δεν μπορεί να είναι κενή.");
        }

        string platformSpecificSubfolder = directoryPath.Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.Combine(_basePath, platformSpecificSubfolder);

        if (!Directory.Exists(fullPath))
        {
            _logger.LogWarning("Attempted to delete a directory that does not exist at: {Path}", fullPath);
            return ServiceResponse.Warning($"Ο φάκελος δεν υπάρχει στη διαδρομή: {fullPath}");
        }

        try
        {
            Directory.Delete(fullPath, true);
            return ServiceResponse.Successful($"Ο φάκελος διαγράφηκε επιτυχώς: {fullPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete directory at: {Path}", fullPath);
            return ServiceResponse.Failure($"Αποτυχία διαγραφής φακέλου: {fullPath}");
        }
    }

    public ServiceResponse MoveDirectory(string sourcePath, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
        {
            return ServiceResponse.Failure("Η διαδρομή του φακέλου δεν μπορεί να είναι κενή.");
        }

        string sourceFullPath = GetFullPath(sourcePath);
        string destinationFullPath = GetFullPath(destinationPath);

        if (!Directory.Exists(sourceFullPath))
        {
            return CreateDirectory(destinationPath);
        }

        if (Directory.Exists(destinationFullPath))
        {
            _logger.LogWarning("Cannot move directory {Source} to {Destination}: destination already exists.",
                sourceFullPath, destinationFullPath);
            return ServiceResponse.Failure($"Ο φάκελος προορισμού υπάρχει ήδη: {destinationFullPath}");
        }

        try
        {
            Directory.Move(sourceFullPath, destinationFullPath);
            return ServiceResponse.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move directory from {Source} to {Destination}", sourceFullPath,
                destinationFullPath);
            return ServiceResponse.Failure($"Αποτυχία μετακίνησης φακέλου: {sourceFullPath}");
        }
    }

    public async Task<ServiceResponse<string>> SaveFileAsync(IFormFile mediaFile, string storagePath,
        string? desiredFileName = null)
    {
        if (mediaFile.Length > 100 * 1024 * 1024)
        {
            return ServiceResponse<string>.Failure("Το μέγεθος του αρχείου δεν μπορεί να υπερβαίνει τα 100 MB.");
        }

        MemoryStream? imageStream = null;
        IFormFile fileToSave = mediaFile;
        string fileExtension = Path.GetExtension(mediaFile.FileName);
        bool isPdf = fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

        if (isPdf)
        {
            try
            {
                using Stream inputStream = mediaFile.OpenReadStream();
                // The old app runs only on Linux (container) and Windows (dev); it goes away at cutover (#28, #63).
#pragma warning disable CA1416
                using SKBitmap bitmap = Conversion.ToImage(inputStream, page: 0);
#pragma warning restore CA1416

                imageStream = new MemoryStream();
                bitmap.Encode(imageStream, SKEncodedImageFormat.Png, quality: 100);
                imageStream.Position = 0;

                string pngFileName = Path.ChangeExtension(mediaFile.FileName, ".png");
                fileToSave = new FormFile(imageStream, 0, imageStream.Length, mediaFile.Name, pngFileName)
                {
                    Headers = mediaFile.Headers,
                    ContentType = "image/png"
                };
                fileExtension = ".png";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert PDF {FileName} to image", mediaFile.FileName);
                return ServiceResponse<string>.Failure("Αποτυχία μετατροπής του PDF σε εικόνα.");
            }
        }

        string storageDirectory = GetFullPath(storagePath);
        if (!Directory.Exists(storageDirectory))
        {
            ServiceResponse createResult = CreateDirectory(storagePath);
            if (!createResult.Success)
            {
                imageStream?.Dispose();
                return ServiceResponse<string>.Failure(createResult.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(desiredFileName))
        {
            string directoryPath = storageDirectory;
            try
            {
                string[] existingFiles = Directory.GetFiles(directoryPath, $"{desiredFileName}.*");
                string[] existingThumbs = Directory.GetFiles(directoryPath, $"thumb_{desiredFileName}.*");
                foreach (string? file in existingFiles.Concat(existingThumbs))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete existing files for {FileName} in {Path}", desiredFileName,
                    directoryPath);
                imageStream?.Dispose();
                return ServiceResponse<string>.Failure($"Αποτυχία διαγραφής υπαρχόντων αρχείων για {desiredFileName}");
            }
        }

        string newFileName = string.IsNullOrWhiteSpace(desiredFileName)
            ? $"{GenerateShortId()}{fileExtension}"
            : $"{desiredFileName}{fileExtension}";

        string fullPath = GetFullPath(Path.Combine(storagePath, newFileName));

        try
        {
            // The stream must be fully written and closed before the thumbnail step
            // reads the file back from disk.
            await using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await fileToSave.CopyToAsync(fileStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save media file at: {Path}", fullPath);
            imageStream?.Dispose();
            return ServiceResponse<string>.Failure($"Αποτυχία αποθήκευσης αρχείου: {fullPath}");
        }

        try
        {
            await GenerateThumbnailAsync(fullPath);
        }
        catch (Exception ex)
        {
            // A failed thumbnail (corrupt or unsupported image) must not fail the upload.
            _logger.LogWarning(ex, "Failed to generate thumbnail for: {Path}", fullPath);
        }
        finally
        {
            imageStream?.Dispose();
        }

        string webPath = $"media/{Path.Combine(storagePath, newFileName).Replace(Path.DirectorySeparatorChar, '/')}";
        return ServiceResponse<string>.Successful(webPath, "Το αρχείο αποθηκεύτηκε επιτυχώς.");
    }

    public ServiceResponse DeleteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Attempted to delete a media file with an empty file path.");
            return ServiceResponse.Failure("Η διαδρομή του αρχείου δεν μπορεί να είναι κενή.");
        }

        string relativePath = filePath.StartsWith("media/") ? filePath[6..] : filePath;
        string fullPath = GetFullPath(relativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Attempted to delete a media file that does not exist at: {Path}", fullPath);
            return ServiceResponse.Warning($"Το αρχείο δεν υπάρχει στη διαδρομή: {fullPath}");
        }

        try
        {
            File.Delete(fullPath);

            string thumbPath = GetThumbnailPath(fullPath);
            if (File.Exists(thumbPath))
            {
                File.Delete(thumbPath);
            }

            return ServiceResponse.Successful($"Το αρχείο διαγράφηκε επιτυχώς: {fullPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media file at: {Path}", fullPath);
            return ServiceResponse.Failure($"Αποτυχία διαγραφής αρχείου: {fullPath}");
        }
    }

    public async Task GenerateMissingThumbnailsAsync()
    {
        if (!Directory.Exists(_basePath))
        {
            return;
        }

        IEnumerable<string> imageFiles = Directory.EnumerateFiles(_basePath, "*.*", SearchOption.AllDirectories)
            .Where(f => IsImageFile(f) && !Path.GetFileName(f).StartsWith("thumb_"));

        int count = 0;

        foreach (string filePath in imageFiles)
        {
            string thumbPath = GetThumbnailPath(filePath);
            if (File.Exists(thumbPath))
            {
                continue;
            }

            try
            {
                await GenerateThumbnailAsync(filePath);
                count++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for: {Path}", filePath);
            }
        }

        if (count > 0)
        {
            _logger.LogInformation("Generated {Count} missing thumbnails", count);
        }
    }

    private async Task GenerateThumbnailAsync(string filePath)
    {
        if (!IsImageFile(filePath))
        {
            return;
        }

        string thumbPath = GetThumbnailPath(filePath);

        using Image image = await Image.LoadAsync(filePath);

        const int maxWidth = 400;
        if (image.Width > maxWidth)
        {
            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(maxWidth, 0) }));
        }

        await image.SaveAsync(thumbPath);
    }

    private static string GetThumbnailPath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath)!;
        string fileName = Path.GetFileName(filePath);
        return Path.Combine(directory, $"thumb_{fileName}");
    }

    private static bool IsImageFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp";
    }

    private string GetFullPath(string subfolder)
    {
        string platformSpecificSubfolder = subfolder.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_basePath, platformSpecificSubfolder);
    }

    private static string GenerateShortId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 11).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public ServiceResponse<MediaStatusViewModel> GetMediaStatus()
    {
        var model = new MediaStatusViewModel();

        if (!Directory.Exists(_basePath))
        {
            return ServiceResponse<MediaStatusViewModel>.Successful(model);
        }

        try
        {
            var dirInfo = new DirectoryInfo(_basePath);

            // Get all files recursively in _basePath to calculate total stats
            FileInfo[] allFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            model.TotalFileCount = allFiles.Length;
            model.TotalSizeBytes = allFiles.Sum(f => f.Length);

            // Get top level directories
            DirectoryInfo[] topDirs = dirInfo.GetDirectories();
            foreach (DirectoryInfo topDir in topDirs)
            {
                FileInfo[] filesInDir = topDir.GetFiles("*", SearchOption.AllDirectories);
                DirectoryInfo[] subDirs = topDir.GetDirectories("*", SearchOption.AllDirectories);

                model.Directories.Add(new DirectoryStatViewModel
                {
                    Name = topDir.Name,
                    RelativePath = topDir.Name,
                    FileCount = filesInDir.Length,
                    SizeBytes = filesInDir.Sum(f => f.Length),
                    SubdirectoryCount = subDirs.Length
                });
            }

            // Also check files directly in the root of _basePath
            FileInfo[] rootFiles = dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
            if (rootFiles.Length > 0)
            {
                model.Directories.Add(new DirectoryStatViewModel
                {
                    Name = "[Ρίζα / Root]",
                    RelativePath = "",
                    FileCount = rootFiles.Length,
                    SizeBytes = rootFiles.Sum(f => f.Length),
                    SubdirectoryCount = 0
                });
            }

            // Order by directory size descending
            model.Directories = model.Directories.OrderByDescending(d => d.SizeBytes).ToList();

            return ServiceResponse<MediaStatusViewModel>.Successful(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media status.");
            return ServiceResponse<MediaStatusViewModel>.Failure("Αποτυχία ανάκτησης κατάστασης πολυμέσων.");
        }
    }
}
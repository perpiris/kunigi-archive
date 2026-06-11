using KunigiArchive.Web.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace KunigiArchive.Web.Services;

public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;
    private readonly string _basePath;

    public DirectoryService(
        IWebHostEnvironment environment,
        ILogger<DirectoryService> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(environment.WebRootPath, "media");
    }

    public ServiceResponse CreateDirectory(string directoryPath)
    {
        var platformSpecificSubfolder = directoryPath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, platformSpecificSubfolder);

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

        var platformSpecificSubfolder = directoryPath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, platformSpecificSubfolder);

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

        var sourceFullPath = GetFullPath(sourcePath);
        var destinationFullPath = GetFullPath(destinationPath);

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

        var fileExtension = Path.GetExtension(mediaFile.FileName);
        var storageDirectory = GetFullPath(storagePath);
        if (!Directory.Exists(storageDirectory))
        {
            var createResult = CreateDirectory(storagePath);
            if (!createResult.Success)
            {
                return ServiceResponse<string>.Failure(createResult.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(desiredFileName))
        {
            var directoryPath = storageDirectory;
            try
            {
                var existingFiles = Directory.GetFiles(directoryPath, $"{desiredFileName}.*");
                var existingThumbs = Directory.GetFiles(directoryPath, $"thumb_{desiredFileName}.*");
                foreach (var file in existingFiles.Concat(existingThumbs))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete existing files for {FileName} in {Path}", desiredFileName,
                    directoryPath);
                return ServiceResponse<string>.Failure($"Αποτυχία διαγραφής υπαρχόντων αρχείων για {desiredFileName}");
            }
        }

        var newFileName = string.IsNullOrWhiteSpace(desiredFileName)
            ? $"{GenerateShortId()}{fileExtension}"
            : $"{desiredFileName}{fileExtension}";

        var fullPath = GetFullPath(Path.Combine(storagePath, newFileName));

        try
        {
            // The stream must be fully written and closed before the thumbnail step
            // reads the file back from disk.
            await using (var fileStream = new FileStream(fullPath, FileMode.Create))
            {
                await mediaFile.CopyToAsync(fileStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save media file at: {Path}", fullPath);
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

        var webPath = $"media/{Path.Combine(storagePath, newFileName).Replace(Path.DirectorySeparatorChar, '/')}";
        return ServiceResponse<string>.Successful(webPath, "Το αρχείο αποθηκεύτηκε επιτυχώς.");
    }

    public ServiceResponse DeleteFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Attempted to delete a media file with an empty file path.");
            return ServiceResponse.Failure("Η διαδρομή του αρχείου δεν μπορεί να είναι κενή.");
        }

        var relativePath = filePath.StartsWith("media/") ? filePath[6..] : filePath;
        var fullPath = GetFullPath(relativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Attempted to delete a media file that does not exist at: {Path}", fullPath);
            return ServiceResponse.Warning($"Το αρχείο δεν υπάρχει στη διαδρομή: {fullPath}");
        }

        try
        {
            File.Delete(fullPath);

            var thumbPath = GetThumbnailPath(fullPath);
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

        var imageFiles = Directory.EnumerateFiles(_basePath, "*.*", SearchOption.AllDirectories)
            .Where(f => IsImageFile(f) && !Path.GetFileName(f).StartsWith("thumb_"));

        var count = 0;

        foreach (var filePath in imageFiles)
        {
            var thumbPath = GetThumbnailPath(filePath);
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

        var thumbPath = GetThumbnailPath(filePath);

        using var image = await Image.LoadAsync(filePath);

        const int maxWidth = 400;
        if (image.Width > maxWidth)
        {
            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(maxWidth, 0) }));
        }

        await image.SaveAsync(thumbPath);
    }

    private static string GetThumbnailPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileName(filePath);
        return Path.Combine(directory, $"thumb_{fileName}");
    }

    private static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp";
    }

    private string GetFullPath(string subfolder)
    {
        var platformSpecificSubfolder = subfolder.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_basePath, platformSpecificSubfolder);
    }

    private static string GenerateShortId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 11).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
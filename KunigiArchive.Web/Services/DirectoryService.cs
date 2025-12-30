using KunigiArchive.Web.Services.Interfaces;

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

    public async Task<ServiceResponse<string>> SaveFileAsync(IFormFile mediaFile, string storagePath,
        string? desiredFileName = null)
    {
        if (mediaFile.Length > 100 * 1024 * 1024)
        {
            return ServiceResponse<string>.Failure("Το μέγεθος του αρχείου δεν μπορεί να υπερβαίνει τα 100 MB.");
        }

        var fileExtension = Path.GetExtension(mediaFile.FileName);
        if (!string.IsNullOrWhiteSpace(desiredFileName))
        {
            var directoryPath = GetFullPath(storagePath);
            try
            {
                var existingFiles = Directory.GetFiles(directoryPath, $"{desiredFileName}.*");
                foreach (var file in existingFiles)
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
            await using var fileStream = new FileStream(fullPath, FileMode.Create);
            await mediaFile.CopyToAsync(fileStream);

            var webPath = $"media/{Path.Combine(storagePath, newFileName).Replace(Path.DirectorySeparatorChar, '/')}";
            return ServiceResponse<string>.Successful(webPath, "Το αρχείο αποθηκεύτηκε επιτυχώς.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save media file at: {Path}", fullPath);
            return ServiceResponse<string>.Failure($"Αποτυχία αποθήκευσης αρχείου: {fullPath}");
        }
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
            return ServiceResponse.Successful($"Το αρχείο διαγράφηκε επιτυχώς: {fullPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media file at: {Path}", fullPath);
            return ServiceResponse.Failure($"Αποτυχία διαγραφής αρχείου: {fullPath}");
        }
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
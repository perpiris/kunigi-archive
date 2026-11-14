using KunigiArchive.Web.ViewModels.Admin;

namespace KunigiArchive.Web.Services.Interfaces;

public interface IDirectoryService
{
    ServiceResponse CreateDirectory(string directoryPath);
    ServiceResponse DeleteDirectory(string directoryPath);
    ServiceResponse MoveDirectory(string sourcePath, string destinationPath);

    Task<ServiceResponse<string>>
        SaveFileAsync(IFormFile mediaFile, string storagePath, string? desiredFileName = null);

    ServiceResponse DeleteFile(string filePath);

    Task GenerateMissingThumbnailsAsync();

    ServiceResponse<MediaStatusViewModel> GetMediaStatus();
}
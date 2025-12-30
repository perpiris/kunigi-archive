namespace KunigiArchive.Web.Services.Interfaces;

public interface IDirectoryService
{
    ServiceResponse CreateDirectory(string directoryPath);
    ServiceResponse DeleteDirectory(string directoryPath);

    Task<ServiceResponse<string>>
        SaveFileAsync(IFormFile mediaFile, string storagePath, string? desiredFileName = null);

    ServiceResponse DeleteFile(string filePath);
}
using KunigiArchive.Application.Media;

namespace KunigiArchive.Web2.Mappers;

public static class MediaMapper
{
    // An empty file input still posts one part without a file name; it isn't an upload.
    // The caller owns the opened streams and disposes them once the handler is done.
    public static List<MediaUpload> ToUploads(this IEnumerable<IFormFile> files)
    {
        return files
            .Where(file => !string.IsNullOrEmpty(file.FileName))
            .Select(file => new MediaUpload(file.OpenReadStream(), file.FileName, file.Length))
            .ToList();
    }
}
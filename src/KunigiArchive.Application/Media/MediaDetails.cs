namespace KunigiArchive.Application.Media;

// One stored file as pages and the API show it. ThumbnailPath is the path itself for a video.
public sealed record MediaDetails(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);

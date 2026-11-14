namespace KunigiArchive.Application.Teams;

public sealed record TeamMediaDetails(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);
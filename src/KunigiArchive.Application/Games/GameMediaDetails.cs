namespace KunigiArchive.Application.Games;

public sealed record GameMediaDetails(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);
namespace KunigiArchive.Web2.Contracts.Teams;

public sealed record TeamMediaResponse(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);
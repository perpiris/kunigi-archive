namespace KunigiArchive.Web2.Contracts.Games;

public sealed record GameMediaResponse(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);
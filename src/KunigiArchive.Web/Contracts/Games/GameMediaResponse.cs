namespace KunigiArchive.Web.Contracts.Games;

public sealed record GameMediaResponse(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);
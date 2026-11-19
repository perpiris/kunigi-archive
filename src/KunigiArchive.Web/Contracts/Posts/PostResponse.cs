namespace KunigiArchive.Web.Contracts.Posts;

// Content is sanitized HTML (rich text).
public sealed record PostResponse(
    Guid Id,
    Guid GameId,
    string? Title,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PostMediaResponse> Media);

public sealed record PostMediaResponse(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);
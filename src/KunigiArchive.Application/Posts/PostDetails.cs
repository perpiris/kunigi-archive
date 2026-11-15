namespace KunigiArchive.Application.Posts;

// Content is sanitized HTML.
public sealed record PostDetails(
    Guid Id,
    Guid GameId,
    string? Title,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PostMediaDetails> Media);

public sealed record PostMediaDetails(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);
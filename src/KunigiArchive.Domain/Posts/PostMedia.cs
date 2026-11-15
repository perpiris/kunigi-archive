namespace KunigiArchive.Domain.Posts;

public sealed class PostMedia
{
    public const int PathMaxLength = 500;
    public const int FileNameMaxLength = 255;

    public Guid Id { get; private set; }
    public Guid PostId { get; private set; }
    public string Path { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    private PostMedia()
    {
    }

    internal PostMedia(Guid postId, string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        PostId = postId;
        Path = path;
        FileName = fileName;
        OriginalFileName = originalFileName;
        UploadedAt = uploadedAt;
    }
}
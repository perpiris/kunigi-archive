namespace KunigiArchive.Domain.Games;

public sealed class GameMedia
{
    public const int PathMaxLength = 500;
    public const int FileNameMaxLength = 255;

    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public string Path { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    private GameMedia()
    {
    }

    internal GameMedia(Guid gameId, string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        GameId = gameId;
        Path = path;
        FileName = fileName;
        OriginalFileName = originalFileName;
        UploadedAt = uploadedAt;
    }
}
namespace KunigiArchive.Domain.GameDays;

public sealed class GameDayMedia
{
    public const int PathMaxLength = 500;
    public const int FileNameMaxLength = 255;

    public Guid Id { get; private set; }
    public Guid GameDayId { get; private set; }
    public string Path { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    private GameDayMedia()
    {
    }

    internal GameDayMedia(Guid gameDayId, string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        GameDayId = gameDayId;
        Path = path;
        FileName = fileName;
        OriginalFileName = originalFileName;
        UploadedAt = uploadedAt;
    }
}

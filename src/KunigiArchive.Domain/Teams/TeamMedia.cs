namespace KunigiArchive.Domain.Teams;

public sealed class TeamMedia
{
    public const int PathMaxLength = 500;
    public const int FileNameMaxLength = 255;

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string Path { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    private TeamMedia()
    {
    }

    internal TeamMedia(Guid teamId, string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        TeamId = teamId;
        Path = path;
        FileName = fileName;
        OriginalFileName = originalFileName;
        UploadedAt = uploadedAt;
    }
}
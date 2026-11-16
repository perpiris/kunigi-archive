namespace KunigiArchive.Domain.GameDays;

// The old app stored "Q" or "A".
public enum PuzzleMediaKind : byte
{
    Question = 1,
    Answer = 2
}

public sealed class PuzzleMedia
{
    public const int PathMaxLength = 500;
    public const int FileNameMaxLength = 255;

    public Guid Id { get; private set; }
    public Guid PuzzleId { get; private set; }
    public PuzzleMediaKind Kind { get; private set; }
    public string Path { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public DateTimeOffset UploadedAt { get; private set; }

    private PuzzleMedia()
    {
    }

    internal PuzzleMedia(Guid puzzleId, PuzzleMediaKind kind, string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        Id = Guid.CreateVersion7();
        PuzzleId = puzzleId;
        Kind = kind;
        Path = path;
        FileName = fileName;
        OriginalFileName = originalFileName;
        UploadedAt = uploadedAt;
    }
}

namespace KunigiArchive.Web.Entities;

public class PuzzleMedia
{
    public long PuzzleMediaId { get; set; }
    public long PuzzleId { get; set; }
    public required string MediaType { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    public virtual Puzzle Puzzle { get; set; } = null!;
}
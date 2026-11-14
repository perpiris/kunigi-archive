namespace KunigiArchive.Web.Entities;

public class GameMedia
{
    public long GameMediaId { get; set; }
    public long GameId { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    public virtual Game Game { get; set; } = null!;
}
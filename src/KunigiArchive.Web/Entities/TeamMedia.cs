namespace KunigiArchive.Web.Entities;

public class TeamMedia
{
    public long TeamMediaId { get; set; }
    public long TeamId { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    public virtual Team Team { get; set; } = null!;
}
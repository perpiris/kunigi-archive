namespace KunigiArchive.Web.Entities;

public class GameDayMedia
{
    public long GameDayMediaId { get; set; }
    public long GameDayId { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    public virtual GameDay GameDay { get; set; } = null!;
}
namespace KunigiArchive.Web.Entities;

public class PostMedia
{
    public long PostMediaId { get; set; }
    public long PostId { get; set; }
    public required string Path { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FileName { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    public virtual Post Post { get; set; } = null!;
}
namespace KunigiArchive.Web.Entities;

public class Post
{
    public long PostId { get; set; }
    public long GameDayId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual GameDay GameDay { get; set; } = null!;
}
namespace KunigiArchive.Web.Entities;

public class Post
{
    public long PostId { get; set; }
    public long GameId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Game Game { get; set; } = null!;
}

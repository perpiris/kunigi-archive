namespace KunigiArchive.Web.Entities;

public class Post
{
    public long PostId { get; set; }
    public long GameId { get; set; }
    public string? Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Game Game { get; set; } = null!;
    public virtual ICollection<PostMedia> MediaFiles { get; set; } = new List<PostMedia>();
}
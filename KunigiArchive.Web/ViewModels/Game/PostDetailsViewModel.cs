namespace KunigiArchive.Web.ViewModels.Game;

public class PostDetailsViewModel
{
    public long PostId { get; set; }
    public long GameId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
namespace KunigiArchive.Web.ViewModels.Game;

public class PostDetailsViewModel
{
    public long PostId { get; set; }
    public long GameId { get; set; }
    public string? Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<KunigiArchive.Web.ViewModels.Common.MediaFileViewModel> Media { get; set; } = [];
}
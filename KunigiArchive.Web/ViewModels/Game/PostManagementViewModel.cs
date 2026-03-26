namespace KunigiArchive.Web.ViewModels.Game;

public class PostManagementViewModel
{
    public long GameId { get; set; }
    public required string GameSlug { get; set; }
    public required string GameFormattedTitle { get; set; }
    public List<PostDetailsViewModel> Posts { get; set; } = [];
}
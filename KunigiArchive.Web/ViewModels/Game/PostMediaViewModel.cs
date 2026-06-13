namespace KunigiArchive.Web.ViewModels.Game;

public class PostMediaViewModel
{
    public long PostId { get; set; }
    public long GameId { get; set; }
    public required string Slug { get; set; }
    public string? PostTitle { get; set; }
    public required string GameFormattedTitle { get; set; }
    public required List<PostMediaItemViewModel> Media { get; set; }
}

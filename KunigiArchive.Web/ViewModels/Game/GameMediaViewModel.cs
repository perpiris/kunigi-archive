namespace KunigiArchive.Web.ViewModels.Game;

public class GameMediaViewModel
{
    public long GameId { get; set; }
    public required string Slug { get; set; }
    public required string FormattedTitle { get; set; }
    public required List<GameMediaItemViewModel> Media { get; set; }
}
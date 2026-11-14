namespace KunigiArchive.Web.ViewModels.Game;

public class GameDaysViewModel
{
    public long GameId { get; set; }
    public required string Slug { get; set; }
    public required string FormattedTitle { get; set; }
    public List<GameDayViewModel> Days { get; set; } = [];
}
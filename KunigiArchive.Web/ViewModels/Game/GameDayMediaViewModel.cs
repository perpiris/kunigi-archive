namespace KunigiArchive.Web.ViewModels.Game;

public class GameDayMediaViewModel
{
    public long GameDayId { get; set; }
    public required string GameSlug { get; set; }
    public required string GameDayName { get; set; }
    public required string GameFormattedTitle { get; set; }
    public required List<GameDayMediaItemViewModel> Media { get; set; }
}
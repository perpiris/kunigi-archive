namespace KunigiArchive.Web.ViewModels.Game;

public class GameDayActionsViewModel
{
    public long GameDayId { get; set; }
    public required string GameSlug { get; set; }
    public required string GameDayName { get; set; }
    public required string GameFormattedTitle { get; set; }
}
namespace KunigiArchive.Web.ViewModels.Game;

public class PuzzleManagementViewModel
{
    public long GameDayId { get; set; }
    public required string GameSlug { get; set; }
    public required string GameDayName { get; set; }
    public required string GameFormattedTitle { get; set; }
    public List<PuzzleDetailsViewModel> Puzzles { get; set; } = [];
}
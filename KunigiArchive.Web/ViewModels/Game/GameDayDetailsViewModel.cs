using KunigiArchive.Web.ViewModels.Common;

namespace KunigiArchive.Web.ViewModels.Game;

public class GameDayDetailsViewModel
{
    public long GameDayId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string GameSlug { get; set; }
    public required string GameFormattedTitle { get; set; }
    public List<PuzzleDetailsViewModel> Puzzles { get; set; } = [];
    public List<MediaFileViewModel> Media { get; set; } = [];
}
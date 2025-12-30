using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Game;

namespace KunigiArchive.Web.ViewModels.Team;

public class TeamDetailsViewModel
{
    public long TeamId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; }
    public short? YearFounded { get; set; }
    public string? Description { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? Youtube { get; set; }
    public string? Website { get; set; }
    public string? Image { get; set; }

    public List<GameDetailsViewModel> GamesWon { get; set; } = [];
    public List<GameDetailsViewModel> GamesHosted { get; set; } = [];
    public List<MediaFileViewModel> Media { get; set; } = [];
}
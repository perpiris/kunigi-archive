namespace KunigiArchive.Web.ViewModels.Team;

public class TeamMediaViewModel
{
    public long TeamId { get; set; }
    public required string TeamName { get; set; }
    public required string Slug { get; set; }

    public required List<TeamMediaItemViewModel> Media { get; set; }
}
namespace KunigiArchive.Web.Entities;

public class Team
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

    public virtual ICollection<TeamManager> Managers { get; set; } = [];
    public virtual ICollection<TeamMedia> MediaFiles { get; set; } = [];
    public virtual ICollection<Game> HostedGames { get; set; } = [];
    public virtual ICollection<Game> WonGames { get; set; } = [];
}
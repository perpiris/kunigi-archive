namespace KunigiArchive.Web.Entities;

public class TeamManager
{
    public long AppUserId { get; set; }
    public long TeamId { get; set; }

    public virtual AppUser AppUser { get; set; } = null!;
    public virtual Team Team { get; set; } = null!;
}
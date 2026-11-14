namespace KunigiArchive.Domain.Teams;

public sealed class TeamManager
{
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }

    private TeamManager()
    {
    }

    internal TeamManager(Guid teamId, Guid userId)
    {
        TeamId = teamId;
        UserId = userId;
    }
}
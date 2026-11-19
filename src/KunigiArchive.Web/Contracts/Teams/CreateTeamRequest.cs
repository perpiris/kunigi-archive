namespace KunigiArchive.Web.Contracts.Teams;

public sealed record CreateTeamRequest
{
    public required string Name { get; init; }
    public bool IsActive { get; init; } = true;
}
namespace KunigiArchive.Web2.Contracts.Teams;

public sealed record AssignTeamManagerRequest
{
    public required Guid UserId { get; init; }
}
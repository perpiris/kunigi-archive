namespace KunigiArchive.Web.Contracts.Teams;

public sealed record AssignTeamManagerRequest
{
    public required Guid UserId { get; init; }
}
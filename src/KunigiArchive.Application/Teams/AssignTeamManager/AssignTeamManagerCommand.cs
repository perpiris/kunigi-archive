namespace KunigiArchive.Application.Teams.AssignTeamManager;

public sealed record AssignTeamManagerCommand(Guid TeamId, Guid UserId);
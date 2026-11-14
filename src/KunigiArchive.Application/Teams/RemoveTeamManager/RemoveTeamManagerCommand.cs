namespace KunigiArchive.Application.Teams.RemoveTeamManager;

public sealed record RemoveTeamManagerCommand(Guid TeamId, Guid UserId);
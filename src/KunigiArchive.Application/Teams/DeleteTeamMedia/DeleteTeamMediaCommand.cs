namespace KunigiArchive.Application.Teams.DeleteTeamMedia;

public sealed record DeleteTeamMediaCommand(Guid TeamId, Guid MediaId);
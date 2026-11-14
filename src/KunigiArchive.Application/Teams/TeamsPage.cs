namespace KunigiArchive.Application.Teams;

public sealed record TeamsPage(IReadOnlyList<TeamSummary> Items, string? NextCursor);
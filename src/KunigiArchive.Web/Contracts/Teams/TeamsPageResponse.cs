namespace KunigiArchive.Web.Contracts.Teams;

public sealed record TeamsPageResponse(IReadOnlyList<TeamSummaryResponse> Items, string? NextCursor);
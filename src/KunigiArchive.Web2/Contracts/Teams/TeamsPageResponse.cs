namespace KunigiArchive.Web2.Contracts.Teams;

public sealed record TeamsPageResponse(IReadOnlyList<TeamSummaryResponse> Items, string? NextCursor);
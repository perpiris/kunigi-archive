namespace KunigiArchive.Web.Contracts.Teams;

public sealed record TeamSummaryResponse(Guid Id, string Name, string Slug, bool IsActive, string? Image);
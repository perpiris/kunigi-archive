namespace KunigiArchive.Application.Teams;

public sealed record TeamSummary(Guid Id, string Name, string Slug, bool IsActive, string? Image);
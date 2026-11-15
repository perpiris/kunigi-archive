namespace KunigiArchive.Application.Games;

public sealed record GameSummary(Guid Id, string Slug, short Year, decimal? Order, string OrderTitle, string? Title);
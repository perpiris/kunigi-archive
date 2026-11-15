namespace KunigiArchive.Web2.Contracts.Games;

public sealed record GameSummaryResponse(Guid Id, string Slug, short Year, decimal? Order, string OrderTitle, string? Title);
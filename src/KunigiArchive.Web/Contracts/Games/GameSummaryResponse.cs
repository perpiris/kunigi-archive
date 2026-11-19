namespace KunigiArchive.Web.Contracts.Games;

public sealed record GameSummaryResponse(Guid Id, string Slug, short Year, decimal? Order, string OrderTitle, string? Title);
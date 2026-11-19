namespace KunigiArchive.Web.Contracts.Games;

public sealed record GamesPageResponse(IReadOnlyList<GameResponse> Items, string? NextCursor);
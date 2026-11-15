namespace KunigiArchive.Web2.Contracts.Games;

public sealed record GamesPageResponse(IReadOnlyList<GameResponse> Items, string? NextCursor);
namespace KunigiArchive.Application.Games;

public sealed record GamesPage(IReadOnlyList<GameListItem> Items, string? NextCursor);
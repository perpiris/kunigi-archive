namespace KunigiArchive.Application.Games.GetGames;

// Search matches the name, title, year, host and winner; the cursor only makes sense with the same search.
public sealed record GetGamesQuery(GamesCursor? After, string? Search)
{
    public const int SearchMaxLength = 100;
}
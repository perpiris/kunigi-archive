namespace KunigiArchive.Application.Games.GetGames;

// Search matches the name, title, year, host and winner; the cursor only makes sense with the same search.
// PageSize is for the admin list, which loads everything so its column sort covers the whole list (#80).
public sealed record GetGamesQuery(GamesCursor? After, string? Search, int? PageSize = null)
{
    public const int SearchMaxLength = 100;
    public const int MaxPageSize = 500;
}
using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Teams;

namespace KunigiArchive.Application.Sitemap;

// The public pages for /sitemap.xml (#27): the lists, every team, every game and each game's enabled days.
// Paths are relative to the site root.
public sealed class GetSitemapHandler
{
    private readonly ITeamQueries _teamQueries;
    private readonly IGameQueries _gameQueries;
    private readonly IGameDayQueries _gameDayQueries;

    public GetSitemapHandler(ITeamQueries teamQueries, IGameQueries gameQueries, IGameDayQueries gameDayQueries)
    {
        _teamQueries = teamQueries;
        _gameQueries = gameQueries;
        _gameDayQueries = gameDayQueries;
    }

    public async Task<IReadOnlyList<string>> HandleAsync(CancellationToken cancellationToken)
    {
        List<string> paths = ["", "teams", "games"];

        IReadOnlyList<TeamRef> teams = await _teamQueries.GetRefsAsync(cancellationToken);
        paths.AddRange(teams.Select(t => $"teams/{t.Slug}"));

        // One query per game for its days: a few dozen games, and crawlers fetch this rarely.
        foreach (GameSummary game in await _gameQueries.GetAllAsync(cancellationToken))
        {
            paths.Add($"games/{game.Slug}");
            IReadOnlyList<GameDaySummary> days = await _gameDayQueries.GetForGameAsync(game.Id, cancellationToken);
            paths.AddRange(days.Where(d => d.IsEnabled).Select(d => $"games/{game.Slug}/days/{d.Slug}"));
        }

        return paths;
    }
}

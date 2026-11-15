using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Teams;

namespace KunigiArchive.Application.Media.GetMediaStatus;

// Admin: how many files each team and game has on disk and how much space they take. Measured from the
// folders themselves (teams/{id}, games/{id} with its posts), so it also shows folders nothing points to.
public sealed class GetMediaStatusHandler
{
    private const string TeamsFolder = "teams";
    private const string GamesFolder = "games";

    private readonly ITeamQueries _teamQueries;
    private readonly IGameQueries _gameQueries;
    private readonly IMediaStorage _mediaStorage;
    private readonly ICurrentUser _currentUser;

    public GetMediaStatusHandler(
        ITeamQueries teamQueries,
        IGameQueries gameQueries,
        IMediaStorage mediaStorage,
        ICurrentUser currentUser)
    {
        _teamQueries = teamQueries;
        _gameQueries = gameQueries;
        _mediaStorage = mediaStorage;
        _currentUser = currentUser;
    }

    public async Task<MediaStatus> HandleAsync(CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        IReadOnlyList<TeamRef> teams = await _teamQueries.GetRefsAsync(cancellationToken);
        IReadOnlyList<GameSummary> games = await _gameQueries.GetAllAsync(cancellationToken);

        IReadOnlyDictionary<string, MediaUsage> teamFolders = _mediaStorage.GetUsageOfSubfolders(TeamsFolder);
        IReadOnlyDictionary<string, MediaUsage> gameFolders = _mediaStorage.GetUsageOfSubfolders(GamesFolder);

        List<MediaStatus.TeamUsage> teamRows = teams
            .Select(team => new MediaStatus.TeamUsage(team, teamFolders.GetValueOrDefault(team.Id.ToString(), MediaUsage.None)))
            .OrderByDescending(row => row.Usage.Bytes)
            .ThenBy(row => row.Team.Name)
            .ToList();

        List<MediaStatus.GameUsage> gameRows = games
            .Select(game => new MediaStatus.GameUsage(game, gameFolders.GetValueOrDefault(game.Id.ToString(), MediaUsage.None)))
            .OrderByDescending(row => row.Usage.Bytes)
            .ThenByDescending(row => row.Game.Year)
            .ToList();

        HashSet<string> teamIds = teams.Select(t => t.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> gameIds = games.Select(g => g.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<MediaStatus.FolderUsage> unknown =
        [
            .. teamFolders.Where(f => !teamIds.Contains(f.Key)).Select(f => new MediaStatus.FolderUsage($"{TeamsFolder}/{f.Key}", f.Value)),
            .. gameFolders.Where(f => !gameIds.Contains(f.Key)).Select(f => new MediaStatus.FolderUsage($"{GamesFolder}/{f.Key}", f.Value))
        ];

        return new MediaStatus(
            _mediaStorage.GetTotalUsage(),
            Sum(teamRows.Select(r => r.Usage)),
            Sum(gameRows.Select(r => r.Usage)),
            teamRows,
            gameRows,
            unknown.OrderByDescending(f => f.Usage.Bytes).ToList());
    }

    private static MediaUsage Sum(IEnumerable<MediaUsage> usages)
    {
        return usages.Aggregate(MediaUsage.None, (total, usage) => new MediaUsage(total.Files + usage.Files, total.Bytes + usage.Bytes));
    }
}

public sealed record MediaStatus(
    MediaUsage Total,
    MediaUsage Teams,
    MediaUsage Games,
    IReadOnlyList<MediaStatus.TeamUsage> TeamRows,
    IReadOnlyList<MediaStatus.GameUsage> GameRows,
    IReadOnlyList<MediaStatus.FolderUsage> UnknownFolders)
{
    public sealed record TeamUsage(TeamRef Team, MediaUsage Usage);

    public sealed record GameUsage(GameSummary Game, MediaUsage Usage);

    // A folder whose team or game no longer exists.
    public sealed record FolderUsage(string Path, MediaUsage Usage);
}
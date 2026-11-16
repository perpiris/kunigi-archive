using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Teams;

namespace KunigiArchive.Application.Games;

// Whatever belongs to a game (posts, days, puzzles) is edited by whoever edits the game: the admin, or a
// manager of the host team.
public sealed class GameAccess
{
    private readonly IGameQueries _gameQueries;
    private readonly ITeamQueries _teamQueries;
    private readonly ICurrentUser _currentUser;

    public GameAccess(IGameQueries gameQueries, ITeamQueries teamQueries, ICurrentUser currentUser)
    {
        _gameQueries = gameQueries;
        _teamQueries = teamQueries;
        _currentUser = currentUser;
    }

    public async Task EnsureCanEditAsync(Guid gameId, CancellationToken cancellationToken)
    {
        GameRecord game = await _gameQueries.GetRecordAsync(gameId, cancellationToken)
            ?? throw new GameNotFoundException(gameId.ToString());

        await _currentUser.EnsureCanEditGameAsync(game.HostId, _teamQueries, cancellationToken);
    }

    // For pages that show more to editors (a disabled day, an edit link) without refusing anyone.
    public async Task<bool> CanEditAsync(Guid? hostId, CancellationToken cancellationToken)
    {
        return await _currentUser.CanEditGameAsync(hostId, _teamQueries, cancellationToken);
    }
}
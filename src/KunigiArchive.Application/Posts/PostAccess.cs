using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Teams;

namespace KunigiArchive.Application.Posts;

// A game's posts are edited by whoever edits the game: the admin, or a manager of the host team.
public sealed class PostAccess
{
    private readonly IGameQueries _gameQueries;
    private readonly ITeamQueries _teamQueries;
    private readonly ICurrentUser _currentUser;

    public PostAccess(IGameQueries gameQueries, ITeamQueries teamQueries, ICurrentUser currentUser)
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
}
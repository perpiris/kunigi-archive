using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Games.GetMyGames;

// The games a manager can edit: those hosted by the teams they manage.
public sealed class GetMyGamesHandler
{
    private readonly IGameQueries _gameQueries;
    private readonly ICurrentUser _currentUser;

    public GetMyGamesHandler(IGameQueries gameQueries, ICurrentUser currentUser)
    {
        _gameQueries = gameQueries;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GameSummary>> HandleAsync(CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.Id ?? throw new ForbiddenException();
        return await _gameQueries.GetHostedByManagerAsync(userId, cancellationToken);
    }
}

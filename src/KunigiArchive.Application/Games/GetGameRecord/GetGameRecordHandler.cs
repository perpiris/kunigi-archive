using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;

namespace KunigiArchive.Application.Games.GetGameRecord;

public sealed class GetGameRecordHandler
{
    private readonly IGameQueries _gameQueries;
    private readonly ICurrentUser _currentUser;

    public GetGameRecordHandler(IGameQueries gameQueries, ICurrentUser currentUser)
    {
        _gameQueries = gameQueries;
        _currentUser = currentUser;
    }

    public async Task<GameRecord> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        return await _gameQueries.GetRecordAsync(id, cancellationToken)
            ?? throw new GameNotFoundException(id.ToString());
    }
}
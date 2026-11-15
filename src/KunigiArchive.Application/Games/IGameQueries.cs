namespace KunigiArchive.Application.Games;

public interface IGameQueries
{
    Task<GamesPage> GetPageAsync(GamesCursor? after, string? search, int pageSize, CancellationToken cancellationToken);
    Task<GameRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken);
}
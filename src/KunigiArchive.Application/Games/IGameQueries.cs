using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Games;

public interface IGameQueries
{
    Task<GamesPage> GetPageAsync(GamesCursor? after, string? search, int pageSize, CancellationToken cancellationToken);
    Task<GameRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<GameSummary>> GetAllAsync(CancellationToken cancellationToken);
    Task<GameDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<GameDetails?> GetDetailsBySlugAsync(Slug slug, CancellationToken cancellationToken);

    // The games hosted by the teams this user manages, newest first.
    Task<IReadOnlyList<GameSummary>> GetHostedByManagerAsync(Guid userId, CancellationToken cancellationToken);
}
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Games;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> AnyHostedOrWonByAsync(Guid teamId, CancellationToken cancellationToken);

    // excludingGameId lets an edit keep its own year, number and slug.
    Task<bool> YearExistsAsync(short year, Guid? excludingGameId, CancellationToken cancellationToken);
    Task<bool> OrderExistsAsync(decimal order, Guid? excludingGameId, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(Slug slug, Guid? excludingGameId, CancellationToken cancellationToken);
    void Add(Game game);
}
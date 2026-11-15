using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Games;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Repositories;

internal sealed class GameRepository : IGameRepository
{
    private readonly KunigiArchiveDbContext _dbContext;

    public GameRepository(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<bool> AnyHostedOrWonByAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await _dbContext.Games.AnyAsync(g => g.HostId == teamId || g.WinnerId == teamId, cancellationToken);
    }

    public async Task<bool> YearExistsAsync(short year, Guid? excludingGameId, CancellationToken cancellationToken)
    {
        return await _dbContext.Games.AnyAsync(g => g.Year == year && g.Id != excludingGameId, cancellationToken);
    }

    public async Task<bool> OrderExistsAsync(decimal order, Guid? excludingGameId, CancellationToken cancellationToken)
    {
        return await _dbContext.Games.AnyAsync(g => g.Order == order && g.Id != excludingGameId, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(Slug slug, Guid? excludingGameId, CancellationToken cancellationToken)
    {
        return await _dbContext.Games.AnyAsync(g => g.Slug == slug && g.Id != excludingGameId, cancellationToken);
    }

    public void Add(Game game)
    {
        _dbContext.Games.Add(game);
    }
}
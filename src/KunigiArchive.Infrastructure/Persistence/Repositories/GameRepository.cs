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

    public async Task<bool> AnyHostedOrWonByAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await _dbContext.Games.AnyAsync(g => g.HostId == teamId || g.WinnerId == teamId, cancellationToken);
    }
}

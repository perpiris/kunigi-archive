using KunigiArchive.Domain.GameDays;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Repositories;

internal sealed class GameDayRepository : IGameDayRepository
{
    private readonly KunigiArchiveDbContext _dbContext;

    public GameDayRepository(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GameDay?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Aggregate().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<GameDay?> GetAsync(Guid gameId, GameDayType type, CancellationToken cancellationToken)
    {
        return await Aggregate().FirstOrDefaultAsync(d => d.GameId == gameId && d.Type == type, cancellationToken);
    }

    public void Add(GameDay day)
    {
        _dbContext.GameDays.Add(day);
    }

    private IQueryable<GameDay> Aggregate()
    {
        return _dbContext.GameDays
            .Include(d => d.Media)
            .Include(d => d.Puzzles).ThenInclude(p => p.Media)
            .AsSplitQuery();
    }
}

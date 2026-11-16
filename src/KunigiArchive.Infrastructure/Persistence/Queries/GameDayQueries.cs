using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.GameDays;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Queries;

internal sealed class GameDayQueries : IGameDayQueries
{
    private readonly KunigiArchiveDbContext _dbContext;

    public GameDayQueries(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GameDaySummary>> GetForGameAsync(Guid gameId, CancellationToken cancellationToken)
    {
        // Counts only: a game page lists its days without loading every puzzle.
        var rows = await _dbContext.GameDays.AsNoTracking()
            .Where(d => d.GameId == gameId)
            .OrderBy(d => d.Type)
            .Select(d => new { d.Id, d.Type, d.IsEnabled, d.Title, Puzzles = d.Puzzles.Count, Media = d.Media.Count })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new GameDaySummary(r.Id, r.Type, r.Type.Label(), r.Type.Slug(), r.IsEnabled, r.Title, r.Puzzles, r.Media))
            .ToList();
    }

    public async Task<GameDayDetails?> GetAsync(Guid gameId, GameDayType type, CancellationToken cancellationToken)
    {
        GameDay? day = await _dbContext.GameDays.AsNoTracking()
            .Include(d => d.Media)
            .Include(d => d.Puzzles).ThenInclude(p => p.Media)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.GameId == gameId && d.Type == type, cancellationToken);

        return day?.ToDetails();
    }
}

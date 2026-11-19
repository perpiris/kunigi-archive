using KunigiArchive.Application.Common;
using KunigiArchive.Domain.GameDays;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence;

// LegacyGameDayIds isn't part of the EF model: a fixed table, filled once from the import's id map by the
// AddLegacyGameDayIds migration and only ever read here (#91).
internal sealed class LegacyDayLinks : ILegacyDayLinks
{
    private readonly KunigiArchiveDbContext _dbContext;

    public LegacyDayLinks(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> FindDayPathAsync(long oldDayId, CancellationToken cancellationToken)
    {
        Row? row = await _dbContext.Database
            .SqlQuery<Row>($"""
                SELECT g.Slug AS GameSlug, d.Type AS DayType
                FROM dbo.LegacyGameDayIds l
                JOIN dbo.GameDays d ON d.Id = l.GameDayId
                JOIN dbo.Games g ON g.Id = d.GameId
                WHERE l.OldId = {oldDayId}
                """)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : $"games/{row.GameSlug}/days/{((GameDayType)row.DayType).Slug()}";
    }

    private sealed record Row(string GameSlug, byte DayType);
}

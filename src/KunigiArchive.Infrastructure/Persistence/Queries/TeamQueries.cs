using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Queries;

internal sealed class TeamQueries : ITeamQueries
{
    private readonly KunigiArchiveDbContext _dbContext;

    public TeamQueries(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeamDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Team? team = await _dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return team?.ToDetails();
    }

    public async Task<TeamDetails?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken)
    {
        Team? team = await _dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

        return team?.ToDetails();
    }
}
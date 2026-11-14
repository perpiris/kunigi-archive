using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Repositories;

internal sealed class TeamRepository : ITeamRepository
{
    private readonly KunigiArchiveDbContext _dbContext;

    public TeamRepository(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> SlugExistsAsync(Slug slug, CancellationToken cancellationToken)
    {
        return await _dbContext.Teams.AnyAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Teams.FindAsync([id], cancellationToken);
    }

    public void Add(Team team)
    {
        _dbContext.Teams.Add(team);
    }
}
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

    public async Task<bool> NameExistsAsync(string name, Guid excludingTeamId, CancellationToken cancellationToken)
    {
        return await _dbContext.Teams.AnyAsync(t => t.Name == name && t.Id != excludingTeamId, cancellationToken);
    }

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Aggregate().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Team?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken)
    {
        return await Aggregate().FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public void Add(Team team)
    {
        _dbContext.Teams.Add(team);
    }

    private IQueryable<Team> Aggregate()
    {
        return _dbContext.Teams
            .Include(t => t.Managers)
            .Include(t => t.Media)
            .AsSplitQuery();
    }
}
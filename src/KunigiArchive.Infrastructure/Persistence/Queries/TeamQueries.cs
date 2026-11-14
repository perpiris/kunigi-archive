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
            .Include(t => t.Media)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return team?.ToDetails();
    }

    public async Task<TeamDetails?> GetBySlugAsync(Slug slug, CancellationToken cancellationToken)
    {
        Team? team = await _dbContext.Teams
            .AsNoTracking()
            .Include(t => t.Media)
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

        return team?.ToDetails();
    }

    public async Task<IReadOnlyList<TeamSummary>> GetManagedByAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<Team> teams = await _dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Managers.Any(m => m.UserId == userId))
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return teams.Select(t => t.ToSummary()).ToList();
    }

    public async Task<TeamsPage> GetPageAsync(TeamsCursor? after, int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<Team> teams = _dbContext.Teams.AsNoTracking();

        if (after is not null)
        {
            string name = after.Name;
            Guid id = after.Id;
            teams = teams.Where(t => string.Compare(t.Name, name) > 0 || (t.Name == name && t.Id > id));
        }

        // One extra row tells whether there is a next page, without a COUNT query.
        List<Team> rows = await teams
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (rows.Count > pageSize)
        {
            rows.RemoveAt(rows.Count - 1);
            Team last = rows[^1];
            nextCursor = new TeamsCursor(last.Name, last.Id).Encode();
        }

        return new TeamsPage(rows.Select(t => t.ToSummary()).ToList(), nextCursor);
    }
}
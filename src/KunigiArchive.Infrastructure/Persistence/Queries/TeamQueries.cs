using KunigiArchive.Application.Games;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Queries;

internal sealed class TeamQueries : ITeamQueries
{
    private const string SearchCollation = "Greek_100_CI_AI";

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

    public async Task<bool> IsManagedByAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Teams
            .AnyAsync(t => t.Id == teamId && t.Managers.Any(m => m.UserId == userId), cancellationToken);
    }

    public async Task<IReadOnlyList<GameSummary>> GetHostedGamesAsync(Guid teamId, CancellationToken cancellationToken)
    {
        List<Game> games = await _dbContext.Games
            .AsNoTracking()
            .Where(g => g.HostId == teamId)
            .OrderByDescending(g => g.Year)
            .ToListAsync(cancellationToken);

        return games.Select(g => g.ToSummary()).ToList();
    }

    public async Task<IReadOnlyList<GameSummary>> GetWonGamesAsync(Guid teamId, CancellationToken cancellationToken)
    {
        List<Game> games = await _dbContext.Games
            .AsNoTracking()
            .Where(g => g.WinnerId == teamId)
            .OrderByDescending(g => g.Year)
            .ToListAsync(cancellationToken);

        return games.Select(g => g.ToSummary()).ToList();
    }

    public async Task<IReadOnlyList<TeamOption>> GetOptionsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Teams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TeamOption(t.Id, t.Name, t.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TeamRef>> GetRefsAsync(CancellationToken cancellationToken)
    {
        List<Team> teams = await _dbContext.Teams.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);
        return teams.Select(t => new TeamRef(t.Id, t.Name, t.Slug.Value)).ToList();
    }

    public async Task<TeamsPage> GetPageAsync(
        TeamsCursor? after,
        string? search,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Team> teams = _dbContext.Teams.AsNoTracking();

        if (search is not null)
        {
            // Accent- and case-insensitive, so "ομαδα" finds "Ομάδα". EF escapes LIKE wildcards in the search text.
            teams = teams.Where(t => EF.Functions.Collate(t.Name, SearchCollation).Contains(search));
        }

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
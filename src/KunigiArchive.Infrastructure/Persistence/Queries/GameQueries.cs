using System.Globalization;
using System.Linq.Expressions;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Queries;

internal sealed class GameQueries : IGameQueries
{
    private const string SearchCollation = "Greek_100_CI_AI";

    private readonly KunigiArchiveDbContext _dbContext;

    public GameQueries(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GamesPage> GetPageAsync(
        GamesCursor? after,
        string? search,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Host and winner are joined by id: teams are their own aggregate, so a game has no navigation to them.
        IQueryable<GameRow> rows =
            from game in _dbContext.Games.AsNoTracking()
            join host in _dbContext.Teams on game.HostId equals host.Id into hosts
            from host in hosts.DefaultIfEmpty()
            join winner in _dbContext.Teams on game.WinnerId equals winner.Id into winners
            from winner in winners.DefaultIfEmpty()
            select new GameRow { Game = game, Host = host, Winner = winner };

        if (search is not null)
        {
            // Accent- and case-insensitive. A number also matches the year ("1998").
            bool isYear = short.TryParse(search, NumberStyles.None, CultureInfo.InvariantCulture, out short year);
            rows = rows.Where(r =>
                (isYear && r.Game.Year == year)
                || EF.Functions.Collate(r.Game.OrderTitle, SearchCollation).Contains(search)
                || (r.Game.Title != null && EF.Functions.Collate(r.Game.Title, SearchCollation).Contains(search))
                || (r.Host != null && EF.Functions.Collate(r.Host.Name, SearchCollation).Contains(search))
                || (r.Winner != null && EF.Functions.Collate(r.Winner.Name, SearchCollation).Contains(search)));
        }

        if (after is not null)
        {
            short afterYear = after.Year;
            Guid afterId = after.Id;
            rows = rows.Where(r => r.Game.Year < afterYear || (r.Game.Year == afterYear && r.Game.Id > afterId));
        }

        // Newest first. One extra row tells whether there is a next page, without a COUNT query.
        List<GameRow> page = await rows
            .OrderByDescending(r => r.Game.Year)
            .ThenBy(r => r.Game.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (page.Count > pageSize)
        {
            page.RemoveAt(page.Count - 1);
            Game last = page[^1].Game;
            nextCursor = new GamesCursor(last.Year, last.Id).Encode();
        }

        return new GamesPage(page.Select(ToListItem).ToList(), nextCursor);
    }

    public async Task<GameRecord?> GetRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        Game? game = await _dbContext.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        return game is null
            ? null
            : new GameRecord(game.Id, game.Slug.Value, game.Year, game.Order, game.OrderTitle, game.HostId, game.WinnerId);
    }

    public async Task<IReadOnlyList<GameSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<Game> games = await _dbContext.Games.AsNoTracking().OrderByDescending(g => g.Year).ToListAsync(cancellationToken);
        return games.Select(g => g.ToSummary()).ToList();
    }

    public async Task<GameDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await GetDetailsAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<GameDetails?> GetDetailsBySlugAsync(Slug slug, CancellationToken cancellationToken)
    {
        return await GetDetailsAsync(g => g.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<GameSummary>> GetHostedByManagerAsync(Guid userId, CancellationToken cancellationToken)
    {
        IQueryable<Guid> managedTeamIds = _dbContext.Teams
            .Where(t => t.Managers.Any(m => m.UserId == userId))
            .Select(t => t.Id);

        List<Game> games = await _dbContext.Games.AsNoTracking()
            .Where(g => g.HostId != null && managedTeamIds.Contains(g.HostId.Value))
            .OrderByDescending(g => g.Year)
            .ToListAsync(cancellationToken);

        return games.Select(g => g.ToSummary()).ToList();
    }

    private async Task<GameDetails?> GetDetailsAsync(Expression<Func<Game, bool>> predicate, CancellationToken cancellationToken)
    {
        Game? game = await _dbContext.Games.AsNoTracking()
            .Include(g => g.Media)
            .FirstOrDefaultAsync(predicate, cancellationToken);

        if (game is null)
        {
            return null;
        }

        List<Guid> teamIds = new[] { game.HostId, game.WinnerId }.OfType<Guid>().ToList();
        Dictionary<Guid, TeamRef> teams = await _dbContext.Teams.AsNoTracking()
            .Where(t => teamIds.Contains(t.Id))
            .Select(t => new TeamRef(t.Id, t.Name, t.Slug.Value))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        return new GameDetails(
            game.Id,
            game.Slug.Value,
            game.Year,
            game.Order,
            game.OrderTitle,
            game.Title,
            game.Note,
            game.Description,
            game.Image,
            game.HostId is Guid hostId ? teams.GetValueOrDefault(hostId) : null,
            game.WinnerId is Guid winnerId ? teams.GetValueOrDefault(winnerId) : null,
            game.Media.OrderByDescending(m => m.UploadedAt).Select(m => m.ToDetails()).ToList());
    }

    private static GameListItem ToListItem(GameRow row)
    {
        Game game = row.Game;
        return new GameListItem(
            game.Id,
            game.Slug.Value,
            game.Year,
            game.Order,
            game.OrderTitle,
            game.Title,
            game.Note,
            game.Image,
            row.Host is null ? null : new TeamRef(row.Host.Id, row.Host.Name, row.Host.Slug.Value),
            row.Winner is null ? null : new TeamRef(row.Winner.Id, row.Winner.Name, row.Winner.Slug.Value));
    }

    // Member initialisation (not a constructor), so EF can still filter and sort on its parts.
    private sealed class GameRow
    {
        public required Game Game { get; init; }
        public Team? Host { get; init; }
        public Team? Winner { get; init; }
    }
}
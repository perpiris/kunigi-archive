using KunigiArchive.Application.Games;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.GameDays;

namespace KunigiArchive.Application.GameDays.GetGameDays;

// The game page's day links: enabled days only.
public sealed class GetGameDaysHandler
{
    private readonly IGameDayQueries _gameDayQueries;

    public GetGameDaysHandler(IGameDayQueries gameDayQueries)
    {
        _gameDayQueries = gameDayQueries;
    }

    public async Task<IReadOnlyList<GameDaySummary>> HandleAsync(Guid gameId, CancellationToken cancellationToken)
    {
        IReadOnlyList<GameDaySummary> days = await _gameDayQueries.GetForGameAsync(gameId, cancellationToken);
        return days.Where(d => d.IsEnabled).ToList();
    }
}

// The days page of the game's management: every kind of day, with the game's day where it has one.
public sealed class GetGameDayOptionsHandler
{
    private readonly IGameDayQueries _gameDayQueries;
    private readonly GameAccess _gameAccess;

    public GetGameDayOptionsHandler(IGameDayQueries gameDayQueries, GameAccess gameAccess)
    {
        _gameDayQueries = gameDayQueries;
        _gameAccess = gameAccess;
    }

    public async Task<IReadOnlyList<GameDayOption>> HandleAsync(Guid gameId, CancellationToken cancellationToken)
    {
        await _gameAccess.EnsureCanEditAsync(gameId, cancellationToken);

        Dictionary<GameDayType, GameDaySummary> days = (await _gameDayQueries.GetForGameAsync(gameId, cancellationToken))
            .ToDictionary(d => d.Type);

        return GameDayTypes.All
            .Select(type => new GameDayOption(type, type.Label(), type.Slug(), days.GetValueOrDefault(type)))
            .ToList();
    }
}

// A day's page with its game. CanEdit tells the page to show edit links and, to editors only, a disabled day.
public sealed record GameDayPage(GameDetails Game, GameDayDetails Day, bool CanEdit);

// The public day page: /games/{game}/days/{day}. A disabled day is not found, except by its editors.
public sealed class GetGameDayHandler
{
    private readonly IGameQueries _gameQueries;
    private readonly IGameDayQueries _gameDayQueries;
    private readonly GameAccess _gameAccess;

    public GetGameDayHandler(IGameQueries gameQueries, IGameDayQueries gameDayQueries, GameAccess gameAccess)
    {
        _gameQueries = gameQueries;
        _gameDayQueries = gameDayQueries;
        _gameAccess = gameAccess;
    }

    public async Task<GameDayPage> HandleAsync(string gameSlug, string daySlug, CancellationToken cancellationToken)
    {
        (GameDetails game, GameDayDetails day) = await GameDayLookup.FindAsync(
            _gameQueries, _gameDayQueries, gameSlug, daySlug, cancellationToken);

        return await ToPageAsync(game, day, daySlug, cancellationToken);
    }

    // The API's way in: by the game's id.
    public async Task<GameDayPage> HandleAsync(Guid gameId, string daySlug, CancellationToken cancellationToken)
    {
        GameDetails game = await _gameQueries.GetDetailsAsync(gameId, cancellationToken)
            ?? throw new GameNotFoundException(gameId.ToString());

        GameDayDetails day = await GameDayLookup.FindDayAsync(_gameDayQueries, game.Id, daySlug, cancellationToken);

        return await ToPageAsync(game, day, daySlug, cancellationToken);
    }

    private async Task<GameDayPage> ToPageAsync(GameDetails game, GameDayDetails day, string daySlug, CancellationToken cancellationToken)
    {
        bool canEdit = await _gameAccess.CanEditAsync(game.Host?.Id, cancellationToken);
        if (!day.IsEnabled && !canEdit)
        {
            throw new GameDayNotFoundException(daySlug);
        }

        return new GameDayPage(game, day, canEdit);
    }
}

// The day's management pages: the admin, or a manager of the host team.
public sealed class GetGameDayForEditHandler
{
    private readonly IGameQueries _gameQueries;
    private readonly IGameDayQueries _gameDayQueries;
    private readonly GameAccess _gameAccess;

    public GetGameDayForEditHandler(IGameQueries gameQueries, IGameDayQueries gameDayQueries, GameAccess gameAccess)
    {
        _gameQueries = gameQueries;
        _gameDayQueries = gameDayQueries;
        _gameAccess = gameAccess;
    }

    public async Task<GameDayPage> HandleAsync(string gameSlug, string daySlug, CancellationToken cancellationToken)
    {
        (GameDetails game, GameDayDetails day) = await GameDayLookup.FindAsync(
            _gameQueries, _gameDayQueries, gameSlug, daySlug, cancellationToken);

        await _gameAccess.EnsureCanEditAsync(game.Id, cancellationToken);

        return new GameDayPage(game, day, CanEdit: true);
    }
}

internal static class GameDayLookup
{
    public static async Task<(GameDetails Game, GameDayDetails Day)> FindAsync(
        IGameQueries gameQueries,
        IGameDayQueries gameDayQueries,
        string gameSlug,
        string daySlug,
        CancellationToken cancellationToken)
    {
        if (!Slug.TryParse(gameSlug, out Slug? parsedSlug))
        {
            throw new GameNotFoundException(gameSlug);
        }

        GameDetails game = await gameQueries.GetDetailsBySlugAsync(parsedSlug, cancellationToken)
            ?? throw new GameNotFoundException(gameSlug);

        return (game, await FindDayAsync(gameDayQueries, game.Id, daySlug, cancellationToken));
    }

    public static async Task<GameDayDetails> FindDayAsync(
        IGameDayQueries gameDayQueries,
        Guid gameId,
        string daySlug,
        CancellationToken cancellationToken)
    {
        if (!GameDayTypes.TryParseSlug(daySlug, out GameDayType? type))
        {
            throw new GameDayNotFoundException(daySlug);
        }

        return await gameDayQueries.GetAsync(gameId, type.Value, cancellationToken)
            ?? throw new GameDayNotFoundException(daySlug);
    }
}

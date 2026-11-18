namespace KunigiArchive.Application.Games.GetGames;

public sealed class GetGamesHandler
{
    private const int PageSize = 24;

    private readonly IGameQueries _gameQueries;

    public GetGamesHandler(IGameQueries gameQueries)
    {
        _gameQueries = gameQueries;
    }

    public async Task<GamesPage> HandleAsync(GetGamesQuery query, CancellationToken cancellationToken)
    {
        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        if (search?.Length > GetGamesQuery.SearchMaxLength)
        {
            search = search[..GetGamesQuery.SearchMaxLength];
        }

        return await _gameQueries.GetPageAsync(query.After, search, Math.Clamp(query.PageSize ?? PageSize, 1, GetGamesQuery.MaxPageSize), cancellationToken);
    }
}
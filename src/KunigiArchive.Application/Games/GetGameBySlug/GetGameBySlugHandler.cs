using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Games.GetGameBySlug;

// The public game page.
public sealed class GetGameBySlugHandler
{
    private readonly IGameQueries _gameQueries;

    public GetGameBySlugHandler(IGameQueries gameQueries)
    {
        _gameQueries = gameQueries;
    }

    public async Task<GameDetails> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        if (!Slug.TryParse(slug, out Slug? parsedSlug))
        {
            throw new GameNotFoundException(slug);
        }

        return await _gameQueries.GetDetailsBySlugAsync(parsedSlug, cancellationToken)
            ?? throw new GameNotFoundException(slug);
    }
}

using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Games.GetGameForEdit;

// The game's management pages: the admin, or a manager of the host team.
public sealed class GetGameForEditHandler
{
    private readonly IGameQueries _gameQueries;
    private readonly ITeamQueries _teamQueries;
    private readonly ICurrentUser _currentUser;

    public GetGameForEditHandler(IGameQueries gameQueries, ITeamQueries teamQueries, ICurrentUser currentUser)
    {
        _gameQueries = gameQueries;
        _teamQueries = teamQueries;
        _currentUser = currentUser;
    }

    public async Task<GameDetails> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        if (!Slug.TryParse(slug, out Slug? parsedSlug))
        {
            throw new GameNotFoundException(slug);
        }

        GameDetails game = await _gameQueries.GetDetailsBySlugAsync(parsedSlug, cancellationToken)
            ?? throw new GameNotFoundException(slug);

        await _currentUser.EnsureCanEditGameAsync(game.Host?.Id, _teamQueries, cancellationToken);

        return game;
    }
}

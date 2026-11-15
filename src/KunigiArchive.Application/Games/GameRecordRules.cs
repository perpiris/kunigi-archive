using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;

namespace KunigiArchive.Application.Games;

// Checks shared by creating a game and correcting its record.
internal static class GameRecordRules
{
    public static async Task EnsureValidAsync(
        Game game,
        Guid? excludingGameId,
        IGameRepository games,
        ITeamRepository teams,
        CancellationToken cancellationToken)
    {
        await EnsureTeamExistsAsync(game.HostId, "διοργανώτρια", teams, cancellationToken);
        await EnsureTeamExistsAsync(game.WinnerId, "νικήτρια", teams, cancellationToken);

        if (await games.YearExistsAsync(game.Year, excludingGameId, cancellationToken))
        {
            throw new GameYearAlreadyTakenException(game.Year);
        }

        if (game.Order is decimal order && await games.OrderExistsAsync(order, excludingGameId, cancellationToken))
        {
            throw new GameOrderAlreadyTakenException(game.OrderTitle);
        }

        if (await games.SlugExistsAsync(game.Slug, excludingGameId, cancellationToken))
        {
            throw new GameSlugAlreadyTakenException(game.Slug);
        }
    }

    private static async Task EnsureTeamExistsAsync(
        Guid? teamId,
        string role,
        ITeamRepository teams,
        CancellationToken cancellationToken)
    {
        if (teamId is Guid id && await teams.GetByIdAsync(id, cancellationToken) is null)
        {
            throw new InvalidRequestException($"Η {role} ομάδα δεν βρέθηκε.");
        }
    }
}
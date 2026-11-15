using KunigiArchive.Application.Games;
using KunigiArchive.Domain.Games;

namespace KunigiArchive.Application.Mappers;

public static class GameMapper
{
    public static GameSummary ToSummary(this Game game)
    {
        return new GameSummary(game.Id, game.Slug.Value, game.Year, game.Order, game.OrderTitle, game.Title);
    }
}

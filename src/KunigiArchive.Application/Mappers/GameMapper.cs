using KunigiArchive.Application.Games;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Games;

namespace KunigiArchive.Application.Mappers;

public static class GameMapper
{
    public static GameSummary ToSummary(this Game game)
    {
        return new GameSummary(game.Id, game.Slug.Value, game.Year, game.Order, game.OrderTitle, game.Title);
    }

    public static GameMediaDetails ToDetails(this GameMedia media)
    {
        return new GameMediaDetails(
            media.Id,
            media.Path,
            MediaRules.ThumbnailFor(media.Path),
            media.OriginalFileName,
            media.UploadedAt);
    }
}
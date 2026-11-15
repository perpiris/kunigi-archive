using KunigiArchive.Application.Games;
using KunigiArchive.Application.Games.CreateGame;
using KunigiArchive.Application.Games.GetGames;
using KunigiArchive.Application.Games.UpdateGameDetails;
using KunigiArchive.Application.Games.UpdateGameRecord;
using KunigiArchive.Web2.Contracts.Games;

namespace KunigiArchive.Web2.Mappers;

public static class GameMapper
{
    public static GetGamesQuery ToQuery(this GetGamesRequest request)
    {
        // The request was validated, so a cursor that is present decodes.
        GamesCursor.TryDecode(request.After, out GamesCursor? after);
        return new GetGamesQuery(after, request.Search);
    }

    public static CreateGameCommand ToCommand(this CreateGameRequest request)
    {
        return new CreateGameCommand(request.Year, request.Order, request.HostId, request.WinnerId);
    }

    public static UpdateGameRecordCommand ToCommand(this UpdateGameRecordRequest request, Guid id)
    {
        return new UpdateGameRecordCommand(id, request.Year, request.Order, request.HostId, request.WinnerId);
    }

    public static GamesPageResponse ToResponse(this GamesPage page)
    {
        return new GamesPageResponse(page.Items.Select(ToResponse).ToList(), page.NextCursor);
    }

    public static GameResponse ToResponse(this GameListItem game)
    {
        return new GameResponse(
            game.Id,
            game.Slug,
            game.Year,
            game.Order,
            game.OrderTitle,
            game.Title,
            game.Note,
            game.Image,
            game.Host?.ToResponse(),
            game.Winner?.ToResponse());
    }

    public static UpdateGameDetailsCommand ToCommand(this UpdateGameDetailsRequest request, Guid id)
    {
        return new UpdateGameDetailsCommand(id, request.Title, request.Note, request.Description);
    }

    public static GameDetailsResponse ToResponse(this GameDetails game)
    {
        return new GameDetailsResponse(
            game.Id,
            game.Slug,
            game.Year,
            game.Order,
            game.OrderTitle,
            game.Title,
            game.Note,
            game.Description,
            game.Image,
            game.Host?.ToResponse(),
            game.Winner?.ToResponse(),
            game.Media.Select(ToResponse).ToList());
    }

    public static GameMediaResponse ToResponse(this GameMediaDetails media)
    {
        return new GameMediaResponse(media.Id, media.Path, media.ThumbnailPath, media.OriginalFileName, media.UploadedAt);
    }

    public static GameSummaryResponse ToResponse(this GameSummary game)
    {
        return new GameSummaryResponse(game.Id, game.Slug, game.Year, game.Order, game.OrderTitle, game.Title);
    }

    private static TeamRefResponse ToResponse(this TeamRef team)
    {
        return new TeamRefResponse(team.Id, team.Name, team.Slug);
    }
}
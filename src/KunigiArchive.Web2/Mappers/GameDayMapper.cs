using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.Media;
using KunigiArchive.Web2.Contracts.GameDays;

namespace KunigiArchive.Web2.Mappers;

public static class GameDayMapper
{
    public static GameDaySummaryResponse ToResponse(this GameDaySummary day)
    {
        return new GameDaySummaryResponse(day.Id, day.Slug, day.Label, day.IsEnabled, day.Title, day.PuzzleCount, day.MediaCount);
    }

    public static GameDayResponse ToResponse(this GameDayDetails day)
    {
        return new GameDayResponse(
            day.Id,
            day.GameId,
            day.Slug,
            day.Label,
            day.IsEnabled,
            day.Title,
            day.Description,
            day.Puzzles.Select(ToResponse).ToList(),
            day.Media.Select(ToResponse).ToList());
    }

    public static PuzzleResponse ToResponse(this PuzzleDetails puzzle)
    {
        return new PuzzleResponse(
            puzzle.Id,
            puzzle.Order,
            puzzle.Group,
            puzzle.Question,
            puzzle.Answer,
            puzzle.QuestionMedia.Select(ToResponse).ToList(),
            puzzle.AnswerMedia.Select(ToResponse).ToList());
    }

    public static MediaResponse ToResponse(this MediaDetails media)
    {
        return new MediaResponse(media.Id, media.Path, media.ThumbnailPath, media.OriginalFileName, media.UploadedAt);
    }
}

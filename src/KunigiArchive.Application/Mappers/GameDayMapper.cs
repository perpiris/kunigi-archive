using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.GameDays;

namespace KunigiArchive.Application.Mappers;

public static class GameDayMapper
{
    public static GameDaySummary ToSummary(this GameDay day)
    {
        return new GameDaySummary(
            day.Id, day.Type, day.Type.Label(), day.Type.Slug(), day.IsEnabled, day.Title, day.Puzzles.Count, day.Media.Count);
    }

    public static GameDayDetails ToDetails(this GameDay day)
    {
        return new GameDayDetails(
            day.Id,
            day.GameId,
            day.Type,
            day.Type.Label(),
            day.Type.Slug(),
            day.IsEnabled,
            day.Title,
            day.Description,
            day.Puzzles.OrderBy(p => p.Order).Select(ToDetails).ToList(),
            day.Media.OrderBy(m => m.UploadedAt).Select(m => ToMediaDetails(m.Id, m.Path, m.OriginalFileName, m.UploadedAt)).ToList());
    }

    public static PuzzleDetails ToDetails(this Puzzle puzzle)
    {
        return new PuzzleDetails(
            puzzle.Id,
            puzzle.Order,
            puzzle.Group,
            puzzle.Question,
            puzzle.Answer,
            Media(puzzle, PuzzleMediaKind.Question),
            Media(puzzle, PuzzleMediaKind.Answer));
    }

    public static MediaDetails ToDetails(this GameDayMedia media)
    {
        return ToMediaDetails(media.Id, media.Path, media.OriginalFileName, media.UploadedAt);
    }

    public static MediaDetails ToDetails(this PuzzleMedia media)
    {
        return ToMediaDetails(media.Id, media.Path, media.OriginalFileName, media.UploadedAt);
    }

    private static List<MediaDetails> Media(Puzzle puzzle, PuzzleMediaKind kind)
    {
        return puzzle.Media
            .Where(m => m.Kind == kind)
            .OrderBy(m => m.UploadedAt)
            .Select(m => m.ToDetails())
            .ToList();
    }

    private static MediaDetails ToMediaDetails(Guid id, string path, string originalFileName, DateTimeOffset uploadedAt)
    {
        return new MediaDetails(id, path, MediaRules.ThumbnailFor(path), originalFileName, uploadedAt);
    }
}

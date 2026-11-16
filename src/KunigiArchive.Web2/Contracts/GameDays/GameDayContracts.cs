using System.ComponentModel.DataAnnotations;
using KunigiArchive.Domain.GameDays;

namespace KunigiArchive.Web2.Contracts.GameDays;

// Day is the day's slug ("xoros", "sabbato", "kuriaki", "diadiktuako", "paidiko", "efibiko").
public sealed record GameDaySummaryResponse(
    Guid Id,
    string Day,
    string Label,
    bool IsEnabled,
    string? Title,
    int PuzzleCount,
    int MediaCount);

// Description, Question and Answer are sanitized HTML (rich text).
public sealed record GameDayResponse(
    Guid Id,
    Guid GameId,
    string Day,
    string Label,
    bool IsEnabled,
    string? Title,
    string? Description,
    IReadOnlyList<PuzzleResponse> Puzzles,
    IReadOnlyList<MediaResponse> Media);

public sealed record PuzzleResponse(
    Guid Id,
    int Order,
    int? Group,
    string? Question,
    string? Answer,
    IReadOnlyList<MediaResponse> QuestionMedia,
    IReadOnlyList<MediaResponse> AnswerMedia);

public sealed record MediaResponse(
    Guid Id,
    string Path,
    string ThumbnailPath,
    string OriginalFileName,
    DateTimeOffset UploadedAt);

public sealed record SetGameDayEnabledRequest
{
    public required bool Enabled { get; init; }
}

// Description is rich text (HTML); it is sanitized on save.
public sealed record GameDayRequest
{
    [StringLength(GameDay.TitleMaxLength)]
    public string? Title { get; init; }

    public string? Description { get; init; }
}

// Question and Answer are rich text (HTML); they are sanitized on save.
public sealed record PuzzleRequest
{
    [Range(1, Puzzle.GroupMax)]
    public int? Group { get; init; }

    public string? Question { get; init; }

    public string? Answer { get; init; }
}

public sealed record MovePuzzleRequest
{
    // true: one place earlier; false: one place later.
    public required bool Up { get; init; }
}

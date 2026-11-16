using KunigiArchive.Application.Media;
using KunigiArchive.Domain.GameDays;

namespace KunigiArchive.Application.GameDays;

// A day in a list: the game page's day links, the management table.
public sealed record GameDaySummary(
    Guid Id,
    GameDayType Type,
    string Label,
    string Slug,
    bool IsEnabled,
    string? Title,
    int PuzzleCount,
    int MediaCount);

// The whole day as its page shows it. Description, Question and Answer are sanitized HTML.
public sealed record GameDayDetails(
    Guid Id,
    Guid GameId,
    GameDayType Type,
    string Label,
    string Slug,
    bool IsEnabled,
    string? Title,
    string? Description,
    IReadOnlyList<PuzzleDetails> Puzzles,
    IReadOnlyList<MediaDetails> Media);

public sealed record PuzzleDetails(
    Guid Id,
    int Order,
    int? Group,
    string? Question,
    string? Answer,
    IReadOnlyList<MediaDetails> QuestionMedia,
    IReadOnlyList<MediaDetails> AnswerMedia);

// One row per kind of day, whether the game has it or not: Day is null for a day never enabled.
public sealed record GameDayOption(GameDayType Type, string Label, string Slug, GameDaySummary? Day);

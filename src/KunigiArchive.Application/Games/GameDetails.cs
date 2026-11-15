namespace KunigiArchive.Application.Games;

/// <summary>A whole game as its page shows it: the record, the host team's text and poster, and the media.</summary>
public sealed record GameDetails(
    Guid Id,
    string Slug,
    short Year,
    decimal? Order,
    string OrderTitle,
    string? Title,
    string? Note,
    string? Description,
    string? Image,
    TeamRef? Host,
    TeamRef? Winner,
    IReadOnlyList<GameMediaDetails> Media);
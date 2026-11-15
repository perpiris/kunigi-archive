namespace KunigiArchive.Application.Games;

/// <summary>What the admin sets for a game: which year and number it was, who hosted and who won.</summary>
public sealed record GameRecord(
    Guid Id,
    string Slug,
    short Year,
    decimal? Order,
    string OrderTitle,
    Guid? HostId,
    Guid? WinnerId);
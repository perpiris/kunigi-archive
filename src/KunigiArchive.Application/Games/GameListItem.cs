namespace KunigiArchive.Application.Games;

public sealed record GameListItem(
    Guid Id,
    string Slug,
    short Year,
    decimal? Order,
    string OrderTitle,
    string? Title,
    string? Note,
    string? Image,
    TeamRef? Host,
    TeamRef? Winner);
namespace KunigiArchive.Web.Contracts.Games;

public sealed record GameResponse(
    Guid Id,
    string Slug,
    short Year,
    decimal? Order,
    string OrderTitle,
    string? Title,
    string? Note,
    string? Image,
    TeamRefResponse? Host,
    TeamRefResponse? Winner);
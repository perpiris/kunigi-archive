namespace KunigiArchive.Web.Contracts.Games;

// Description is sanitized HTML (rich text).
public sealed record GameDetailsResponse(
    Guid Id,
    string Slug,
    short Year,
    decimal? Order,
    string OrderTitle,
    string? Title,
    string? Note,
    string? Description,
    string? Image,
    TeamRefResponse? Host,
    TeamRefResponse? Winner,
    IReadOnlyList<GameMediaResponse> Media);
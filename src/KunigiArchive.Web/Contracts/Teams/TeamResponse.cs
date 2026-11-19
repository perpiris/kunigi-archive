namespace KunigiArchive.Web.Contracts.Teams;

public sealed record TeamResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    short? YearFounded,
    string? Description,
    string? Facebook,
    string? Instagram,
    string? Youtube,
    string? Website,
    string? Image,
    IReadOnlyList<TeamMediaResponse> Media);
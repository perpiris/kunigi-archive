namespace KunigiArchive.Application.Teams;

public sealed record TeamDetails(
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
    string? Image);
namespace KunigiArchive.Application.Teams.UpdateTeam;

// Slug is optional: null keeps the current one. Only admins may change it.
public sealed record UpdateTeamCommand(
    Guid Id,
    string Name,
    string? Slug,
    bool IsActive,
    short? YearFounded,
    string? Description,
    string? Facebook,
    string? Instagram,
    string? Youtube,
    string? Website);
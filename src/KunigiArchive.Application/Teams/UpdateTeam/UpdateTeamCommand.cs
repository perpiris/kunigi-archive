namespace KunigiArchive.Application.Teams.UpdateTeam;

public sealed record UpdateTeamCommand(
    Guid Id,
    string Name,
    bool IsActive,
    short? YearFounded,
    string? Description,
    string? Facebook,
    string? Instagram,
    string? Youtube,
    string? Website);
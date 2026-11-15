using KunigiArchive.Application.Teams;

namespace KunigiArchive.Application.Users;

// IsCurrentUser: admins can't change their own role, so the page doesn't offer it.
public sealed record UserDetails(
    Guid Id,
    string Email,
    string? Role,
    IReadOnlyList<TeamSummary> ManagedTeams,
    bool IsCurrentUser);

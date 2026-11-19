using KunigiArchive.Web.Contracts.Teams;

namespace KunigiArchive.Web.Contracts.Users;

public sealed record UserDetailsResponse(
    Guid Id,
    string Email,
    string? Role,
    IReadOnlyList<TeamSummaryResponse> ManagedTeams,
    bool IsCurrentUser);

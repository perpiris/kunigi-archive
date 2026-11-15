using KunigiArchive.Web2.Contracts.Teams;

namespace KunigiArchive.Web2.Contracts.Users;

public sealed record UserDetailsResponse(
    Guid Id,
    string Email,
    string? Role,
    IReadOnlyList<TeamSummaryResponse> ManagedTeams,
    bool IsCurrentUser);

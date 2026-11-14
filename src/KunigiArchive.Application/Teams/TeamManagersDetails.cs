using KunigiArchive.Application.Users;

namespace KunigiArchive.Application.Teams;

public sealed record TeamManagersDetails(
    Guid TeamId,
    string TeamName,
    string Slug,
    IReadOnlyList<UserSummary> Managers,
    IReadOnlyList<UserSummary> AvailableManagers);
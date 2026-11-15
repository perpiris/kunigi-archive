namespace KunigiArchive.Web2.Contracts.Users;

public sealed record UsersPageResponse(IReadOnlyList<UserResponse> Items, string? NextCursor);

namespace KunigiArchive.Web.Contracts.Users;

public sealed record UsersPageResponse(IReadOnlyList<UserResponse> Items, string? NextCursor);

namespace KunigiArchive.Application.Users;

public sealed record UsersPage(IReadOnlyList<UserAccount> Items, string? NextCursor);

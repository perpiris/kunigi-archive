namespace KunigiArchive.Application.Users.ChangeUserRole;

// Role is one of Roles.All, or null to remove the user's role.
public sealed record ChangeUserRoleCommand(Guid UserId, string? Role);

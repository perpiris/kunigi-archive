namespace KunigiArchive.Application.Users.CreateUser;

// Role is one of Roles.All, or null for a user without a role.
public sealed record CreateUserCommand(string Email, string? Role);

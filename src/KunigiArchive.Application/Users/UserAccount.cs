namespace KunigiArchive.Application.Users;

// Role is null for a user without one: they can sign in but manage nothing.
public sealed record UserAccount(Guid Id, string Email, string? Role);

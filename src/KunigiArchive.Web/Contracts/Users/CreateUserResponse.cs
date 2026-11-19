namespace KunigiArchive.Web.Contracts.Users;

// The email with the temporary password is sent in the background; POST .../temporary-password sends a new one.
public sealed record CreateUserResponse(Guid Id);

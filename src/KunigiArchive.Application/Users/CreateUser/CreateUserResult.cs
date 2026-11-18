namespace KunigiArchive.Application.Users.CreateUser;

// The email with the temporary password is queued, not sent yet (#20).
public sealed record CreateUserResult(Guid UserId);

namespace KunigiArchive.Application.Users.CreateUser;

// EmailSent is false when the account exists but the email with the temporary password failed;
// the admin can then send a new temporary password from the user's page.
public sealed record CreateUserResult(Guid UserId, bool EmailSent);

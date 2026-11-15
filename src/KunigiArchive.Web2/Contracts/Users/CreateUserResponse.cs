namespace KunigiArchive.Web2.Contracts.Users;

// EmailSent false: the account exists but the email failed; POST .../temporary-password sends a new one.
public sealed record CreateUserResponse(Guid Id, bool EmailSent);

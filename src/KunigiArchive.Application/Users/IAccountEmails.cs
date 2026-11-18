namespace KunigiArchive.Application.Users;

// Queues the account emails and returns at once; they're sent in the background, with retries (#20).
// A send that keeps failing is logged as an error; the admin can send a new temporary password.
public interface IAccountEmails
{
    Task SendAccountCreatedAsync(string email, string temporaryPassword, CancellationToken cancellationToken);
    Task SendTemporaryPasswordAsync(string email, string temporaryPassword, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(string email, string code, CancellationToken cancellationToken);
}

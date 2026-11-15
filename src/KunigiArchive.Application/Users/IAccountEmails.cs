namespace KunigiArchive.Application.Users;

// Throws EmailDeliveryException when the email can't be sent.
public interface IAccountEmails
{
    Task SendAccountCreatedAsync(string email, string temporaryPassword, CancellationToken cancellationToken);
    Task SendTemporaryPasswordAsync(string email, string temporaryPassword, CancellationToken cancellationToken);
}

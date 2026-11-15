using KunigiArchive.Application.Users.ResetPassword;

namespace KunigiArchive.Application.Users;

// Writes to the Identity store. Each call saves on its own (UserManager does), so callers that combine
// several wrap them in IUnitOfWork.ExecuteInTransactionAsync.
public interface IUserAccounts
{
    // Throws InvalidRequestException when Identity rejects the email or password.
    Task<Guid> CreateAsync(string email, string password, string? role, CancellationToken cancellationToken);

    // Also renews the security stamp, so the user's open sessions pick up the new role.
    Task SetRoleAsync(Guid userId, string? role, CancellationToken cancellationToken);

    // Replaces the password, ends the user's open sessions and clears any lockout.
    Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    // Null when no account has this email.
    Task<PasswordResetCode?> CreatePasswordResetCodeAsync(string email, CancellationToken cancellationToken);

    // Sets the password from a reset code, ends the user's open sessions and clears any lockout. Returns the
    // user's id, or null when the email or code is wrong or expired. Throws InvalidRequestException when the
    // password breaks the rules; the old password still works then.
    Task<Guid?> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken);
}

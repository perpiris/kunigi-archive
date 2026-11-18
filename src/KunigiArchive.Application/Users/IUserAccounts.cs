using KunigiArchive.Application.Users.ResetPassword;

namespace KunigiArchive.Application.Users;

// Writes to the Identity store. Each call saves on its own (UserManager does), so callers that combine
// several wrap them in IUnitOfWork.ExecuteInTransactionAsync.
public interface IUserAccounts
{
    // Creates the account with a temporary password: the user has to choose their own after signing in.
    // Throws InvalidRequestException when Identity rejects the email or password.
    Task<Guid> CreateAsync(string email, string password, string? role, CancellationToken cancellationToken);

    // Also renews the security stamp, so the user's open sessions pick up the new role.
    Task SetRoleAsync(Guid userId, string? role, CancellationToken cancellationToken);

    // A temporary password from an admin: replaces the password, ends the user's open sessions, clears any
    // lockout, and makes the user choose their own password after signing in.
    Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    // A disabled account can't sign in or reset its password. Renews the security stamp, so the user's open
    // sessions end within the stamp check interval (a minute).
    Task SetDisabledAsync(Guid userId, bool disabled, CancellationToken cancellationToken);

    // Deletes the account; its team-manager rows go with it (cascade).
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);

    // The user's own changes, confirmed with their current password. Both renew the security stamp, so the
    // user's other sessions end. Throw InvalidRequestException for a wrong current password, or an email or
    // password Identity rejects.
    Task ChangeEmailAsync(Guid userId, string currentPassword, string newEmail, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    // Null when no account has this email, or the account is disabled.
    Task<PasswordResetCode?> CreatePasswordResetCodeAsync(string email, CancellationToken cancellationToken);

    // Sets the password from a reset code, ends the user's open sessions and clears any lockout. Returns the
    // user's id, or null when the email or code is wrong or expired, or the account is disabled. Throws
    // InvalidRequestException when the password breaks the rules; the old password still works then.
    Task<Guid?> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken cancellationToken);
}
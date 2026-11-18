using System.Text;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Users;
using KunigiArchive.Application.Users.ResetPassword;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace KunigiArchive.Infrastructure.Identity;

internal sealed class UserAccounts : IUserAccounts
{
    private readonly UserManager<AppUser> _userManager;

    public UserAccounts(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Guid> CreateAsync(string email, string password, string? role, CancellationToken cancellationToken)
    {
        // Admins create the accounts and the password goes to that address, so it counts as confirmed.
        AppUser user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, MustChangePassword = true };

        EnsureSucceeded(await _userManager.CreateAsync(user, password));

        if (role is not null)
        {
            EnsureSucceeded(await _userManager.AddToRoleAsync(user, role));
        }

        return user.Id;
    }

    public async Task SetRoleAsync(Guid userId, string? role, CancellationToken cancellationToken)
    {
        AppUser user = await FindAsync(userId);

        EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, await _userManager.GetRolesAsync(user)));

        if (role is not null)
        {
            EnsureSucceeded(await _userManager.AddToRoleAsync(user, role));
        }

        // Roles live in the sign-in cookie; a new stamp makes the security stamp check sign the user out.
        EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));
    }

    public async Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        AppUser user = await FindAsync(userId);

        // ResetPasswordAsync also renews the security stamp, which ends the user's open sessions.
        string token = await _userManager.GeneratePasswordResetTokenAsync(user);
        EnsureSucceeded(await _userManager.ResetPasswordAsync(user, token, password));

        EnsureSucceeded(await _userManager.SetLockoutEndDateAsync(user, null));
        EnsureSucceeded(await _userManager.ResetAccessFailedCountAsync(user));

        user.MustChangePassword = true;
        EnsureSucceeded(await _userManager.UpdateAsync(user));
    }

    public async Task SetDisabledAsync(Guid userId, bool disabled, CancellationToken cancellationToken)
    {
        AppUser user = await FindAsync(userId);
        user.IsDisabled = disabled;
        EnsureSucceeded(await _userManager.UpdateAsync(user));

        // Sign-in refuses a disabled account (DisabledUserConfirmation); a new stamp also ends open sessions.
        EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        AppUser user = await FindAsync(userId);
        EnsureSucceeded(await _userManager.DeleteAsync(user));
    }

    public async Task ChangeEmailAsync(Guid userId, string currentPassword, string newEmail, CancellationToken cancellationToken)
    {
        AppUser user = await FindAsync(userId);
        await EnsurePasswordAsync(user, currentPassword);

        // The email is also the user name people sign in with. SetEmailAsync renews the security stamp.
        EnsureSucceeded(await _userManager.SetUserNameAsync(user, newEmail));
        EnsureSucceeded(await _userManager.SetEmailAsync(user, newEmail));

        // Admins create accounts for addresses they know; a user's own change is trusted the same way.
        user.EmailConfirmed = true;
        EnsureSucceeded(await _userManager.UpdateAsync(user));
    }

    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        AppUser user = await FindAsync(userId);
        await EnsurePasswordAsync(user, currentPassword);

        // Checks the rules before changing anything, and renews the security stamp.
        IdentityResult result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            throw new InvalidRequestException(PasswordRules.Message);
        }

        await ClearMustChangePasswordAsync(user);
    }

    // The user chose their own password: a temporary one no longer has to be replaced.
    private async Task ClearMustChangePasswordAsync(AppUser user)
    {
        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            EnsureSucceeded(await _userManager.UpdateAsync(user));
        }
    }

    private async Task EnsurePasswordAsync(AppUser user, string password)
    {
        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            throw new InvalidRequestException(WrongPasswordMessage);
        }
    }

    private const string WrongPasswordMessage = "Ο τρέχων κωδικός δεν είναι σωστός.";

    public async Task<PasswordResetCode?> CreatePasswordResetCodeAsync(string email, CancellationToken cancellationToken)
    {
        AppUser? user = await _userManager.FindByEmailAsync(email);
        if (user?.Email is null || user.IsDisabled)
        {
            return null;
        }

        // Identity's token holds the security stamp: it expires (TokenLifespan) and dies once the password changes.
        string token = await _userManager.GeneratePasswordResetTokenAsync(user);
        string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return new PasswordResetCode(user.Id, user.Email, code);
    }

    public async Task<Guid?> ResetPasswordAsync(
        string email, string code, string newPassword, CancellationToken cancellationToken)
    {
        AppUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null || user.IsDisabled || !TryDecode(code, out string token))
        {
            return null;
        }

        IdentityResult result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                return null;
            }

            throw new InvalidRequestException(PasswordRules.Message);
        }

        EnsureSucceeded(await _userManager.SetLockoutEndDateAsync(user, null));
        EnsureSucceeded(await _userManager.ResetAccessFailedCountAsync(user));
        await ClearMustChangePasswordAsync(user);

        return user.Id;
    }

    private static bool TryDecode(string code, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }

    private async Task<AppUser> FindAsync(Guid userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString()) ?? throw new UserNotFoundException(userId);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }
}
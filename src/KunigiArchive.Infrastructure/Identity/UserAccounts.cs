using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Users;
using Microsoft.AspNetCore.Identity;

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
        AppUser user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };

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

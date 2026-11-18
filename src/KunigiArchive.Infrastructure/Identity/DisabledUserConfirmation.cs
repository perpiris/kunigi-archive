using Microsoft.AspNetCore.Identity;

namespace KunigiArchive.Infrastructure.Identity;

// SignInManager asks this before every sign-in (SignIn.RequireConfirmedAccount): a disabled account gets
// SignInResult.NotAllowed (#24). Every account's email counts as confirmed, so being enabled is all it checks.
internal sealed class DisabledUserConfirmation : IUserConfirmation<AppUser>
{
    public Task<bool> IsConfirmedAsync(UserManager<AppUser> manager, AppUser user)
    {
        return Task.FromResult(!user.IsDisabled);
    }
}

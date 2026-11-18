using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KunigiArchive.Infrastructure.Identity;

public static class AppClaims
{
    // On the sign-in cookie while the user still has a temporary password (#24); Web2 sends them to their
    // account page until they choose their own.
    public const string MustChangePassword = "kunigi:must_change_password";
}

// The usual user and role claims, plus AppClaims.MustChangePassword. The security stamp check rebuilds the
// cookie's claims with this, and so does RefreshSignInAsync after the user changes their password.
internal sealed class AppUserClaimsFactory(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser, AppRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        if (user.MustChangePassword)
        {
            identity.AddClaim(new Claim(AppClaims.MustChangePassword, "true"));
        }

        return identity;
    }
}

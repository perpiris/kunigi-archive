using Microsoft.AspNetCore.Identity;

namespace KunigiArchive.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    // Set by an admin: the account can't sign in or reset its password until it's enabled again (#24).
    public bool IsDisabled { get; set; }

    // Set with every temporary password (new account, or one an admin sends): the user has to choose their own
    // password before using the site. Cleared when they do (#24).
    public bool MustChangePassword { get; set; }
}

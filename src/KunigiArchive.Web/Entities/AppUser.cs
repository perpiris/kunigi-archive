using Microsoft.AspNetCore.Identity;

namespace KunigiArchive.Web.Entities;

public class AppUser : IdentityUser<long>
{
    public virtual ICollection<TeamManager> TeamManagers { get; set; } = [];
}
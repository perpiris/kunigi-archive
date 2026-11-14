using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Domain.Teams;

namespace KunigiArchive.Application.Authorization;

public static class CurrentUserExtensions
{
    public static bool IsAdmin(this ICurrentUser user)
    {
        return user.IsInRole(Roles.Admin);
    }

    public static void EnsureAdmin(this ICurrentUser user)
    {
        if (!user.IsAdmin())
        {
            throw new ForbiddenException();
        }
    }

    public static bool CanEdit(this ICurrentUser user, Team team)
    {
        return user.IsAdmin() || (user.Id is Guid id && user.IsInRole(Roles.Manager) && team.IsManagedBy(id));
    }

    public static void EnsureCanEdit(this ICurrentUser user, Team team)
    {
        if (!user.CanEdit(team))
        {
            throw new ForbiddenException();
        }
    }
}
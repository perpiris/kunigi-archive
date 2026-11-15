using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Teams;
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

    // A game belongs to its host team: the admin, or a manager of the host team. A game without a host is the admin's.
    public static async Task<bool> CanEditGameAsync(
        this ICurrentUser user, Guid? hostId, ITeamQueries teamQueries, CancellationToken cancellationToken)
    {
        if (user.IsAdmin())
        {
            return true;
        }

        return hostId is Guid teamId
            && user.Id is Guid userId
            && user.IsInRole(Roles.Manager)
            && await teamQueries.IsManagedByAsync(teamId, userId, cancellationToken);
    }

    public static async Task EnsureCanEditGameAsync(
        this ICurrentUser user, Guid? hostId, ITeamQueries teamQueries, CancellationToken cancellationToken)
    {
        if (!await user.CanEditGameAsync(hostId, teamQueries, cancellationToken))
        {
            throw new ForbiddenException();
        }
    }
}
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Users;
using KunigiArchive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Identity;

internal sealed class UserQueries : IUserQueries
{
    private readonly KunigiArchiveDbContext _dbContext;

    public UserQueries(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserSummary>> GetManagersAsync(CancellationToken cancellationToken)
    {
        return await UsersInRole(Roles.Manager)
            .OrderBy(u => u.Email)
            .Select(u => new UserSummary(u.Id, u.Email!))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSummary>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _dbContext.Users
            .Where(u => ids.Contains(u.Id))
            .OrderBy(u => u.Email)
            .Select(u => new UserSummary(u.Id, u.Email!))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsManagerAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await UsersInRole(Roles.Manager).AnyAsync(u => u.Id == userId, cancellationToken);
    }

    private IQueryable<AppUser> UsersInRole(string role)
    {
        string normalizedRole = role.ToUpperInvariant();

        return from user in _dbContext.Users
               join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
               join appRole in _dbContext.Roles on userRole.RoleId equals appRole.Id
               where appRole.NormalizedName == normalizedRole
               select user;
    }
}
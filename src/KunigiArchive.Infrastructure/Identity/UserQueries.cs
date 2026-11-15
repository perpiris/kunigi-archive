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

    public async Task<UserAccount?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await Accounts(_dbContext.Users.Where(u => u.Id == id)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        string normalizedEmail = email.ToUpperInvariant();
        return await _dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<UsersPage> GetPageAsync(
        UsersCursor? after,
        string? search,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AppUser> users = _dbContext.Users.AsNoTracking();

        if (search is not null)
        {
            string normalizedSearch = search.ToUpperInvariant();
            users = users.Where(u => u.NormalizedEmail!.Contains(normalizedSearch));
        }

        if (after is not null)
        {
            string email = after.Email;
            Guid id = after.Id;
            users = users.Where(u => string.Compare(u.Email, email) > 0 || (u.Email == email && u.Id > id));
        }

        // One extra row tells whether there is a next page, without a COUNT query.
        List<UserAccount> rows = await Accounts(users.OrderBy(u => u.Email).ThenBy(u => u.Id))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (rows.Count > pageSize)
        {
            rows.RemoveAt(rows.Count - 1);
            nextCursor = new UsersCursor(rows[^1].Email, rows[^1].Id).Encode();
        }

        return new UsersPage(rows, nextCursor);
    }

    // One role per user, so the first (only) role name is the user's role.
    private IQueryable<UserAccount> Accounts(IQueryable<AppUser> users)
    {
        return users.Select(u => new UserAccount(
            u.Id,
            u.Email!,
            _dbContext.UserRoles
                .Where(ur => ur.UserId == u.Id)
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .FirstOrDefault()));
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
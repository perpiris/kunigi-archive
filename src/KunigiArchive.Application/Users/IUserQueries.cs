namespace KunigiArchive.Application.Users;

public interface IUserQueries
{
    Task<IReadOnlyList<UserSummary>> GetManagersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> IsManagerAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserAccount?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<UsersPage> GetPageAsync(UsersCursor? after, string? search, int pageSize, CancellationToken cancellationToken);
}

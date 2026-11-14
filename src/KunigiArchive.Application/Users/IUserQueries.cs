namespace KunigiArchive.Application.Users;

public interface IUserQueries
{
    Task<IReadOnlyList<UserSummary>> GetManagersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> IsManagerAsync(Guid userId, CancellationToken cancellationToken);
}
namespace KunigiArchive.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    // For work that saves more than once, e.g. Identity's UserManager saves on every call.
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken);
}

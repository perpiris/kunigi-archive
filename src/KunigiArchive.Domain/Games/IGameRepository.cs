namespace KunigiArchive.Domain.Games;

public interface IGameRepository
{
    Task<bool> AnyHostedOrWonByAsync(Guid teamId, CancellationToken cancellationToken);
}

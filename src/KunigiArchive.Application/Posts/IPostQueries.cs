namespace KunigiArchive.Application.Posts;

public interface IPostQueries
{
    // Newest first.
    Task<IReadOnlyList<PostDetails>> GetForGameAsync(Guid gameId, CancellationToken cancellationToken);
    Task<PostDetails?> GetAsync(Guid id, CancellationToken cancellationToken);
}
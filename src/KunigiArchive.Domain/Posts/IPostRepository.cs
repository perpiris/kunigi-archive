namespace KunigiArchive.Domain.Posts;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Post post);
    void Remove(Post post);
}
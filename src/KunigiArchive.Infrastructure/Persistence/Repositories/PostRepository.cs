using KunigiArchive.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Repositories;

internal sealed class PostRepository : IPostRepository
{
    private readonly KunigiArchiveDbContext _dbContext;

    public PostRepository(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Posts.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public void Add(Post post)
    {
        _dbContext.Posts.Add(post);
    }

    public void Remove(Post post)
    {
        _dbContext.Posts.Remove(post);
    }
}
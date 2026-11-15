using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Posts;
using KunigiArchive.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Infrastructure.Persistence.Queries;

internal sealed class PostQueries : IPostQueries
{
    private readonly KunigiArchiveDbContext _dbContext;

    public PostQueries(KunigiArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PostDetails>> GetForGameAsync(Guid gameId, CancellationToken cancellationToken)
    {
        List<Post> posts = await _dbContext.Posts.AsNoTracking()
            .Include(p => p.Media)
            .Where(p => p.GameId == gameId)
            .OrderByDescending(p => p.CreatedAt)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return posts.Select(p => p.ToDetails()).ToList();
    }

    public async Task<PostDetails?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        Post? post = await _dbContext.Posts.AsNoTracking()
            .Include(p => p.Media)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return post?.ToDetails();
    }
}
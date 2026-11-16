using KunigiArchive.Application.Games;
using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Posts;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Posts.DeletePost;

public sealed class DeletePostHandler
{
    private readonly IPostRepository _postRepository;
    private readonly GameAccess _gameAccess;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePostHandler> _logger;

    public DeletePostHandler(
        IPostRepository postRepository,
        GameAccess gameAccess,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ILogger<DeletePostHandler> logger)
    {
        _postRepository = postRepository;
        _gameAccess = gameAccess;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        Post post = await _postRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new PostNotFoundException(id);

        await _gameAccess.EnsureCanEditAsync(post.GameId, cancellationToken);

        // Media rows go with the post (cascade); the files after, like every other delete.
        _postRepository.Remove(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _mediaStorage.DeleteDirectory(PostMediaPaths.Directory(post.GameId, post.Id));

        _logger.LogInformation("Post {PostId} deleted from game {GameId}", post.Id, post.GameId);
    }
}
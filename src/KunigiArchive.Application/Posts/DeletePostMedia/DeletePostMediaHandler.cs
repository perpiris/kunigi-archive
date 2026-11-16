using KunigiArchive.Application.Games;
using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Posts;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Posts.DeletePostMedia;

public sealed class DeletePostMediaHandler
{
    private readonly IPostRepository _postRepository;
    private readonly GameAccess _gameAccess;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePostMediaHandler> _logger;

    public DeletePostMediaHandler(
        IPostRepository postRepository,
        GameAccess gameAccess,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ILogger<DeletePostMediaHandler> logger)
    {
        _postRepository = postRepository;
        _gameAccess = gameAccess;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(DeletePostMediaCommand command, CancellationToken cancellationToken)
    {
        Post post = await _postRepository.GetByIdAsync(command.PostId, cancellationToken)
            ?? throw new PostNotFoundException(command.PostId);

        await _gameAccess.EnsureCanEditAsync(post.GameId, cancellationToken);

        PostMedia media = post.RemoveMedia(command.MediaId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The row goes first: a leftover file is harmless, a row pointing at a deleted file is a broken image.
        _mediaStorage.Delete(media.Path);

        _logger.LogInformation("Media {MediaId} deleted from post {PostId}", media.Id, post.Id);
    }
}

public sealed record DeletePostMediaCommand(Guid PostId, Guid MediaId);
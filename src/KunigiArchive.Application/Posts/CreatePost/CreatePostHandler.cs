using KunigiArchive.Application.Games;
using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Posts;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Posts.CreatePost;

public sealed class CreatePostHandler
{
    private readonly IPostRepository _postRepository;
    private readonly GameAccess _gameAccess;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CreatePostHandler> _logger;

    public CreatePostHandler(
        IPostRepository postRepository,
        GameAccess gameAccess,
        IRichTextSanitizer sanitizer,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<CreatePostHandler> logger)
    {
        _postRepository = postRepository;
        _gameAccess = gameAccess;
        _sanitizer = sanitizer;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<PostDetails> HandleAsync(CreatePostCommand command, CancellationToken cancellationToken)
    {
        await _gameAccess.EnsureCanEditAsync(command.GameId, cancellationToken);

        Post post = Post.Create(command.GameId, command.Title, _sanitizer.Sanitize(command.Content), _timeProvider.GetUtcNow());
        _postRepository.Add(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Post {PostId} created for game {GameId}", post.Id, post.GameId);

        return post.ToDetails();
    }
}

public sealed record CreatePostCommand(Guid GameId, string? Title, string? Content);
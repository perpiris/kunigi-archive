using KunigiArchive.Application.Games;
using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Posts;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Posts.UpdatePost;

public sealed class UpdatePostHandler
{
    private readonly IPostRepository _postRepository;
    private readonly GameAccess _gameAccess;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePostHandler> _logger;

    public UpdatePostHandler(
        IPostRepository postRepository,
        GameAccess gameAccess,
        IRichTextSanitizer sanitizer,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePostHandler> logger)
    {
        _postRepository = postRepository;
        _gameAccess = gameAccess;
        _sanitizer = sanitizer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PostDetails> HandleAsync(UpdatePostCommand command, CancellationToken cancellationToken)
    {
        Post post = await _postRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new PostNotFoundException(command.Id);

        await _gameAccess.EnsureCanEditAsync(post.GameId, cancellationToken);

        post.Update(command.Title, _sanitizer.Sanitize(command.Content));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Post {PostId} updated", post.Id);

        return post.ToDetails();
    }
}

public sealed record UpdatePostCommand(Guid Id, string? Title, string? Content);
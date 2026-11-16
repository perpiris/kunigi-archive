using KunigiArchive.Application.Games;

namespace KunigiArchive.Application.Posts.GetPostForEdit;

public sealed class GetPostForEditHandler
{
    private readonly IPostQueries _postQueries;
    private readonly GameAccess _gameAccess;

    public GetPostForEditHandler(IPostQueries postQueries, GameAccess gameAccess)
    {
        _postQueries = postQueries;
        _gameAccess = gameAccess;
    }

    public async Task<PostDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        PostDetails post = await _postQueries.GetAsync(id, cancellationToken)
            ?? throw new PostNotFoundException(id);

        await _gameAccess.EnsureCanEditAsync(post.GameId, cancellationToken);

        return post;
    }
}
namespace KunigiArchive.Application.Posts.GetGamePosts;

// A game's posts, newest first. Public: they show on the game page.
public sealed class GetGamePostsHandler
{
    private readonly IPostQueries _postQueries;

    public GetGamePostsHandler(IPostQueries postQueries)
    {
        _postQueries = postQueries;
    }

    public async Task<IReadOnlyList<PostDetails>> HandleAsync(Guid gameId, CancellationToken cancellationToken)
    {
        return await _postQueries.GetForGameAsync(gameId, cancellationToken);
    }
}
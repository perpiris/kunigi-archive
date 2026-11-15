namespace KunigiArchive.Application.Posts.GetPostForEdit;

public sealed class GetPostForEditHandler
{
    private readonly IPostQueries _postQueries;
    private readonly PostAccess _postAccess;

    public GetPostForEditHandler(IPostQueries postQueries, PostAccess postAccess)
    {
        _postQueries = postQueries;
        _postAccess = postAccess;
    }

    public async Task<PostDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        PostDetails post = await _postQueries.GetAsync(id, cancellationToken)
            ?? throw new PostNotFoundException(id);

        await _postAccess.EnsureCanEditAsync(post.GameId, cancellationToken);

        return post;
    }
}
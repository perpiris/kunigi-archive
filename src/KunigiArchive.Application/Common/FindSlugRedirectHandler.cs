namespace KunigiArchive.Application.Common;

// For a page whose slug wasn't found: where that team or game is now, if it had this address before.
public sealed class FindSlugRedirectHandler
{
    private readonly ISlugHistory _slugHistory;

    public FindSlugRedirectHandler(ISlugHistory slugHistory)
    {
        _slugHistory = slugHistory;
    }

    public async Task<string?> HandleAsync(SlugOwner owner, string previousSlug, CancellationToken cancellationToken)
    {
        return await _slugHistory.FindCurrentSlugAsync(owner, previousSlug, cancellationToken);
    }
}

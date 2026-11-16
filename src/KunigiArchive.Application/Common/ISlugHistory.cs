namespace KunigiArchive.Application.Common;

public enum SlugOwner : byte
{
    Team = 1,
    Game = 2
}

// The addresses teams and games had before: a link to an old one is answered with a permanent redirect to the
// current one, so links shared before a rename (or from the old site) keep working.
public interface ISlugHistory
{
    // Saved with the change that replaced the slug (same unit of work). A string, not a Slug: the import also
    // records the old site's addresses, which the old generator didn't always normalize ("a--b").
    Task RecordAsync(SlugOwner owner, string previous, Guid ownerId, CancellationToken cancellationToken);

    // The current slug of whatever used to be at this address, or null.
    Task<string?> FindCurrentSlugAsync(SlugOwner owner, string previous, CancellationToken cancellationToken);
}

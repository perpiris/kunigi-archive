namespace KunigiArchive.Application.Common;

// The old MVC site linked a game day as /games/{slug}/day/{oldId} (its numeric id). Those ids were mapped to the
// new days when the old data was imported (table LegacyGameDayIds), so old links keep working (#91).
public interface ILegacyDayLinks
{
    // The day's current address ("games/34o-kunigi-thisaurou/days/kuriaki"), or null for an unknown id.
    Task<string?> FindDayPathAsync(long oldDayId, CancellationToken cancellationToken);
}

public sealed class FindLegacyDayPathHandler
{
    private readonly ILegacyDayLinks _legacyDayLinks;

    public FindLegacyDayPathHandler(ILegacyDayLinks legacyDayLinks)
    {
        _legacyDayLinks = legacyDayLinks;
    }

    public async Task<string?> HandleAsync(long oldDayId, CancellationToken cancellationToken)
    {
        return await _legacyDayLinks.FindDayPathAsync(oldDayId, cancellationToken);
    }
}

namespace KunigiArchive.Application.Teams.GetTeams;

// Search filters by name; the cursor only makes sense together with the same search.
// PageSize is for the admin list, which loads everything so its column sort covers the whole list (#80).
public sealed record GetTeamsQuery(TeamsCursor? After, string? Search, int? PageSize = null)
{
    public const int SearchMaxLength = 100;
    public const int MaxPageSize = 500;
}

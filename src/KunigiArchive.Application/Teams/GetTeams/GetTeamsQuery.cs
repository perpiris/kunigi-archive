namespace KunigiArchive.Application.Teams.GetTeams;

// Search filters by name; the cursor only makes sense together with the same search.
public sealed record GetTeamsQuery(TeamsCursor? After, string? Search)
{
    public const int SearchMaxLength = 100;
}

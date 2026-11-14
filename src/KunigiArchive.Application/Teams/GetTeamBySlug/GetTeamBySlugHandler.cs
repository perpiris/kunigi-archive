using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Teams.GetTeamBySlug;

public sealed class GetTeamBySlugHandler
{
    private readonly ITeamQueries _teamQueries;

    public GetTeamBySlugHandler(ITeamQueries teamQueries)
    {
        _teamQueries = teamQueries;
    }

    public async Task<TeamDetails> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        // A malformed slug can't belong to any team, so it's a 404 rather than a validation error.
        if (!Slug.TryParse(slug, out Slug? parsedSlug))
        {
            throw new TeamNotFoundException(slug);
        }

        return await _teamQueries.GetBySlugAsync(parsedSlug, cancellationToken)
            ?? throw new TeamNotFoundException(slug);
    }
}
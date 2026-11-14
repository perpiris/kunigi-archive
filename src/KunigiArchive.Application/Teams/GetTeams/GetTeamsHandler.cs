namespace KunigiArchive.Application.Teams.GetTeams;

public sealed class GetTeamsHandler
{
    private const int PageSize = 20;

    private readonly ITeamQueries _teamQueries;

    public GetTeamsHandler(ITeamQueries teamQueries)
    {
        _teamQueries = teamQueries;
    }

    public async Task<TeamsPage> HandleAsync(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        return await _teamQueries.GetPageAsync(query.After, PageSize, cancellationToken);
    }
}
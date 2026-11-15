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
        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        if (search?.Length > GetTeamsQuery.SearchMaxLength)
        {
            search = search[..GetTeamsQuery.SearchMaxLength];
        }

        return await _teamQueries.GetPageAsync(query.After, search, PageSize, cancellationToken);
    }
}
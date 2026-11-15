namespace KunigiArchive.Application.Teams.GetTeamOptions;

public sealed class GetTeamOptionsHandler
{
    private readonly ITeamQueries _teamQueries;

    public GetTeamOptionsHandler(ITeamQueries teamQueries)
    {
        _teamQueries = teamQueries;
    }

    public async Task<IReadOnlyList<TeamOption>> HandleAsync(CancellationToken cancellationToken)
    {
        return await _teamQueries.GetOptionsAsync(cancellationToken);
    }
}
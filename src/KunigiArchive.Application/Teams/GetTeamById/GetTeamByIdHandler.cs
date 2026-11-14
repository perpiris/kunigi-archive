namespace KunigiArchive.Application.Teams.GetTeamById;

public sealed class GetTeamByIdHandler
{
    private readonly ITeamQueries _teamQueries;

    public GetTeamByIdHandler(ITeamQueries teamQueries)
    {
        _teamQueries = teamQueries;
    }

    public async Task<TeamDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _teamQueries.GetByIdAsync(id, cancellationToken)
            ?? throw new TeamNotFoundException(id.ToString());
    }
}
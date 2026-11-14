using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Teams.GetMyTeams;

public sealed class GetMyTeamsHandler
{
    private readonly ITeamQueries _teamQueries;
    private readonly ICurrentUser _currentUser;

    public GetMyTeamsHandler(ITeamQueries teamQueries, ICurrentUser currentUser)
    {
        _teamQueries = teamQueries;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TeamSummary>> HandleAsync(CancellationToken cancellationToken)
    {
        Guid userId = _currentUser.Id ?? throw new ForbiddenException();
        return await _teamQueries.GetManagedByAsync(userId, cancellationToken);
    }
}
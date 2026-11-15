using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Games;
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Teams.GetTeamProfile;

public sealed class GetTeamProfileHandler
{
    private readonly ITeamQueries _teamQueries;
    private readonly ICurrentUser _currentUser;

    public GetTeamProfileHandler(ITeamQueries teamQueries, ICurrentUser currentUser)
    {
        _teamQueries = teamQueries;
        _currentUser = currentUser;
    }

    public async Task<TeamProfile> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        if (!Slug.TryParse(slug, out Slug? parsedSlug))
        {
            throw new TeamNotFoundException(slug);
        }

        TeamDetails team = await _teamQueries.GetBySlugAsync(parsedSlug, cancellationToken)
            ?? throw new TeamNotFoundException(slug);

        IReadOnlyList<GameSummary> hosted = await _teamQueries.GetHostedGamesAsync(team.Id, cancellationToken);
        IReadOnlyList<GameSummary> won = await _teamQueries.GetWonGamesAsync(team.Id, cancellationToken);

        return new TeamProfile(team, hosted, won, await CanEditAsync(team.Id, cancellationToken));
    }

    private async Task<bool> CanEditAsync(Guid teamId, CancellationToken cancellationToken)
    {
        if (_currentUser.IsAdmin())
        {
            return true;
        }

        return _currentUser.Id is Guid userId
            && _currentUser.IsInRole(Roles.Manager)
            && await _teamQueries.IsManagedByAsync(teamId, userId, cancellationToken);
    }
}

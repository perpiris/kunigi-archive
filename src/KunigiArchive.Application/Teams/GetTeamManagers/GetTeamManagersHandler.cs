using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Users;
using KunigiArchive.Domain.Common;
using KunigiArchive.Domain.Teams;

namespace KunigiArchive.Application.Teams.GetTeamManagers;

public sealed class GetTeamManagersHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserQueries _userQueries;
    private readonly ICurrentUser _currentUser;

    public GetTeamManagersHandler(ITeamRepository teamRepository, IUserQueries userQueries, ICurrentUser currentUser)
    {
        _teamRepository = teamRepository;
        _userQueries = userQueries;
        _currentUser = currentUser;
    }

    public async Task<TeamManagersDetails> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        if (!Slug.TryParse(slug, out Slug? parsedSlug))
        {
            throw new TeamNotFoundException(slug);
        }

        Team team = await _teamRepository.GetBySlugAsync(parsedSlug, cancellationToken)
            ?? throw new TeamNotFoundException(slug);

        HashSet<Guid> managerIds = team.Managers.Select(m => m.UserId).ToHashSet();
        IReadOnlyList<UserSummary> managers = await _userQueries.GetByIdsAsync(managerIds, cancellationToken);
        IReadOnlyList<UserSummary> available = (await _userQueries.GetManagersAsync(cancellationToken))
            .Where(u => !managerIds.Contains(u.Id))
            .ToList();

        return team.ToManagersDetails(managers, available);
    }
}
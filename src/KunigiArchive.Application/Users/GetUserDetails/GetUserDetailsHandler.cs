using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Teams;

namespace KunigiArchive.Application.Users.GetUserDetails;

public sealed class GetUserDetailsHandler
{
    private readonly IUserQueries _userQueries;
    private readonly ITeamQueries _teamQueries;
    private readonly ICurrentUser _currentUser;

    public GetUserDetailsHandler(IUserQueries userQueries, ITeamQueries teamQueries, ICurrentUser currentUser)
    {
        _userQueries = userQueries;
        _teamQueries = teamQueries;
        _currentUser = currentUser;
    }

    public async Task<UserDetails> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        UserAccount user = await _userQueries.GetAsync(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        IReadOnlyList<TeamSummary> teams = await _teamQueries.GetManagedByAsync(user.Id, cancellationToken);

        return new UserDetails(user.Id, user.Email, user.Role, user.IsDisabled, teams, user.Id == _currentUser.Id);
    }
}

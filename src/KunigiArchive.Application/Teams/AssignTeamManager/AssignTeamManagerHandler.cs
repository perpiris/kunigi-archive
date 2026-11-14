using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Users;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.AssignTeamManager;

public sealed class AssignTeamManagerHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUserQueries _userQueries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AssignTeamManagerHandler> _logger;

    public AssignTeamManagerHandler(
        ITeamRepository teamRepository,
        IUserQueries userQueries,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<AssignTeamManagerHandler> logger)
    {
        _teamRepository = teamRepository;
        _userQueries = userQueries;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(AssignTeamManagerCommand command, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        Team team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken)
            ?? throw new TeamNotFoundException(command.TeamId.ToString());

        if (!await _userQueries.IsManagerAsync(command.UserId, cancellationToken))
        {
            throw new UserIsNotManagerException();
        }

        team.AssignManager(command.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} assigned as manager of team {TeamId}", command.UserId, team.Id);
    }
}
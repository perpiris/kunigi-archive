using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.RemoveTeamManager;

public sealed class RemoveTeamManagerHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RemoveTeamManagerHandler> _logger;

    public RemoveTeamManagerHandler(
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<RemoveTeamManagerHandler> logger)
    {
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(RemoveTeamManagerCommand command, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        Team team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken)
            ?? throw new TeamNotFoundException(command.TeamId.ToString());

        team.RemoveManager(command.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} removed as manager of team {TeamId}", command.UserId, team.Id);
    }
}
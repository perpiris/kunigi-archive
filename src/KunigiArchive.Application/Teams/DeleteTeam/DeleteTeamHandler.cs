using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.DeleteTeam;

public sealed class DeleteTeamHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteTeamHandler> _logger;

    public DeleteTeamHandler(
        ITeamRepository teamRepository,
        IGameRepository gameRepository,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeleteTeamHandler> logger)
    {
        _teamRepository = teamRepository;
        _gameRepository = gameRepository;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(Guid teamId, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        Team team = await _teamRepository.GetByIdAsync(teamId, cancellationToken)
            ?? throw new TeamNotFoundException(teamId.ToString());

        // A game keeps its host and winner, so those teams stay. The foreign keys enforce it too.
        if (await _gameRepository.AnyHostedOrWonByAsync(team.Id, cancellationToken))
        {
            throw new TeamHasGamesException(team.Name);
        }

        // Managers and media rows are removed with the team (cascade).
        _teamRepository.Remove(team);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Files go after the rows, like single media deletes: leftover files are harmless, missing ones are not.
        _mediaStorage.DeleteDirectory(TeamMediaPaths.Directory(team.Id));

        _logger.LogInformation("Team {TeamId} ({TeamSlug}) deleted", team.Id, team.Slug.Value);
    }
}

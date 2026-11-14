using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.DeleteTeamMedia;

public sealed class DeleteTeamMediaHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteTeamMediaHandler> _logger;

    public DeleteTeamMediaHandler(
        ITeamRepository teamRepository,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeleteTeamMediaHandler> logger)
    {
        _teamRepository = teamRepository;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteTeamMediaCommand command, CancellationToken cancellationToken)
    {
        Team team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken)
            ?? throw new TeamNotFoundException(command.TeamId.ToString());

        _currentUser.EnsureCanEdit(team);

        TeamMedia media = team.RemoveMedia(command.MediaId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The row goes first: a leftover file is harmless, a row pointing at a deleted file is a broken image.
        _mediaStorage.Delete(media.Path);

        _logger.LogInformation("Media {MediaId} deleted from team {TeamId}", media.Id, team.Id);
    }
}
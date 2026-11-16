using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.SetTeamImage;

public sealed class SetTeamImageHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SetTeamImageHandler> _logger;

    public SetTeamImageHandler(
        ITeamRepository teamRepository,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<SetTeamImageHandler> logger)
    {
        _teamRepository = teamRepository;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TeamDetails> HandleAsync(SetTeamImageCommand command, CancellationToken cancellationToken)
    {
        Team team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken)
            ?? throw new TeamNotFoundException(command.TeamId.ToString());

        _currentUser.EnsureCanEdit(team);

        StoredMedia? stored = null;
        if (command.Image is not null)
        {
            MediaRules.ValidateImage(command.Image);
            stored = await _mediaStorage.SaveAsync(TeamMediaPaths.Directory(team.Id), command.Image, cancellationToken);
        }

        string? previous;
        try
        {
            previous = team.SetImage(stored?.Path);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (stored is not null)
            {
                _mediaStorage.Delete(stored.Path);
            }

            throw;
        }

        if (previous is not null)
        {
            _mediaStorage.Delete(previous);
        }

        _logger.LogInformation("Team {TeamId} image {Change}", team.Id, stored is null ? "removed" : "replaced");

        return team.ToDetails();
    }
}

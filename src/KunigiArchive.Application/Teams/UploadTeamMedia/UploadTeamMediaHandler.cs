using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Teams.UploadTeamMedia;

public sealed class UploadTeamMediaHandler
{
    private readonly ITeamRepository _teamRepository;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UploadTeamMediaHandler> _logger;

    public UploadTeamMediaHandler(
        ITeamRepository teamRepository,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        ILogger<UploadTeamMediaHandler> logger)
    {
        _teamRepository = teamRepository;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TeamMediaDetails>> HandleAsync(
        UploadTeamMediaCommand command,
        CancellationToken cancellationToken)
    {
        Team team = await _teamRepository.GetByIdAsync(command.TeamId, cancellationToken)
            ?? throw new TeamNotFoundException(command.TeamId.ToString());

        _currentUser.EnsureCanEdit(team);

        if (command.Files.Count == 0)
        {
            throw new InvalidRequestException("Δεν επιλέχθηκαν αρχεία.");
        }

        // Check every file before writing any, so one bad file doesn't leave the others half uploaded.
        foreach (MediaUpload file in command.Files)
        {
            MediaRules.Validate(file);
        }

        List<StoredMedia> stored = [];
        List<TeamMedia> added = [];
        try
        {
            foreach (MediaUpload file in command.Files)
            {
                StoredMedia media = await _mediaStorage.SaveAsync(TeamDirectory(team), file, cancellationToken);
                stored.Add(media);
                added.Add(team.AddMedia(media.Path, media.FileName, OriginalName(file), _timeProvider.GetUtcNow()));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (StoredMedia media in stored)
            {
                _mediaStorage.Delete(media.Path);
            }

            throw;
        }

        _logger.LogInformation("{Count} media files uploaded to team {TeamId}", added.Count, team.Id);

        return added.Select(m => m.ToDetails()).ToList();
    }

    // By id, not slug: the folder never has to move when an admin changes the slug.
    private static string TeamDirectory(Team team)
    {
        return $"teams/{team.Id}";
    }

    private static string OriginalName(MediaUpload file)
    {
        string name = Path.GetFileName(file.FileName);
        return name.Length <= TeamMedia.FileNameMaxLength ? name : name[..TeamMedia.FileNameMaxLength];
    }
}
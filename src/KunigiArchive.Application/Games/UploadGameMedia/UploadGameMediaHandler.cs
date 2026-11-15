using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Games;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Games.UploadGameMedia;

public sealed class UploadGameMediaHandler
{
    private readonly IGameRepository _gameRepository;
    private readonly ITeamQueries _teamQueries;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UploadGameMediaHandler> _logger;

    public UploadGameMediaHandler(
        IGameRepository gameRepository,
        ITeamQueries teamQueries,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        ILogger<UploadGameMediaHandler> logger)
    {
        _gameRepository = gameRepository;
        _teamQueries = teamQueries;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameMediaDetails>> HandleAsync(
        UploadGameMediaCommand command,
        CancellationToken cancellationToken)
    {
        Game game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken)
            ?? throw new GameNotFoundException(command.GameId.ToString());

        await _currentUser.EnsureCanEditGameAsync(game.HostId, _teamQueries, cancellationToken);

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
        List<GameMedia> added = [];
        try
        {
            foreach (MediaUpload file in command.Files)
            {
                StoredMedia media = await _mediaStorage.SaveAsync(GameMediaPaths.Directory(game.Id), file, cancellationToken);
                stored.Add(media);
                added.Add(game.AddMedia(media.Path, media.FileName, OriginalName(file), _timeProvider.GetUtcNow()));
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

        _logger.LogInformation("{Count} media files uploaded to game {GameId}", added.Count, game.Id);

        return added.Select(m => m.ToDetails()).ToList();
    }

    private static string OriginalName(MediaUpload file)
    {
        string name = Path.GetFileName(file.FileName);
        return name.Length <= GameMedia.FileNameMaxLength ? name : name[..GameMedia.FileNameMaxLength];
    }
}

public sealed record UploadGameMediaCommand(Guid GameId, IReadOnlyList<MediaUpload> Files);

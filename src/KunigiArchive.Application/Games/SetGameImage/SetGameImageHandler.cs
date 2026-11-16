using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Games;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Games.SetGameImage;

// The game's poster. A null image removes the current one.
public sealed class SetGameImageHandler
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameQueries _gameQueries;
    private readonly ITeamQueries _teamQueries;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SetGameImageHandler> _logger;

    public SetGameImageHandler(
        IGameRepository gameRepository,
        IGameQueries gameQueries,
        ITeamQueries teamQueries,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<SetGameImageHandler> logger)
    {
        _gameRepository = gameRepository;
        _gameQueries = gameQueries;
        _teamQueries = teamQueries;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<GameDetails> HandleAsync(SetGameImageCommand command, CancellationToken cancellationToken)
    {
        Game game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken)
            ?? throw new GameNotFoundException(command.GameId.ToString());

        await _currentUser.EnsureCanEditGameAsync(game.HostId, _teamQueries, cancellationToken);

        StoredMedia? stored = null;
        if (command.Image is not null)
        {
            MediaRules.ValidateImage(command.Image);
            stored = await _mediaStorage.SaveAsync(GameMediaPaths.Directory(game.Id), command.Image, cancellationToken);
        }

        string? previous;
        try
        {
            previous = game.SetImage(stored?.Path);
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

        _logger.LogInformation("Game {GameId} poster {Change}", game.Id, stored is null ? "removed" : "replaced");

        return (await _gameQueries.GetDetailsAsync(game.Id, cancellationToken))!;
    }
}

public sealed record SetGameImageCommand(Guid GameId, MediaUpload? Image);

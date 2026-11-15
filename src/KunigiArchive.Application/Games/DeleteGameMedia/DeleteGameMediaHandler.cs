using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Games;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Games.DeleteGameMedia;

public sealed class DeleteGameMediaHandler
{
    private readonly IGameRepository _gameRepository;
    private readonly ITeamQueries _teamQueries;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteGameMediaHandler> _logger;

    public DeleteGameMediaHandler(
        IGameRepository gameRepository,
        ITeamQueries teamQueries,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeleteGameMediaHandler> logger)
    {
        _gameRepository = gameRepository;
        _teamQueries = teamQueries;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteGameMediaCommand command, CancellationToken cancellationToken)
    {
        Game game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken)
            ?? throw new GameNotFoundException(command.GameId.ToString());

        await _currentUser.EnsureCanEditGameAsync(game.HostId, _teamQueries, cancellationToken);

        GameMedia media = game.RemoveMedia(command.MediaId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The row goes first: a leftover file is harmless, a row pointing at a deleted file is a broken image.
        _mediaStorage.Delete(media.Path);

        _logger.LogInformation("Media {MediaId} deleted from game {GameId}", media.Id, game.Id);
    }
}

public sealed record DeleteGameMediaCommand(Guid GameId, Guid MediaId);

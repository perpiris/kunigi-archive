using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Games;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Games.DeleteGame;

// Admin only (#24): the game, its media rows (cascade) and its media folder.
public sealed class DeleteGameHandler
{
    private readonly IGameRepository _gameRepository;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<DeleteGameHandler> _logger;

    public DeleteGameHandler(
        IGameRepository gameRepository,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<DeleteGameHandler> logger)
    {
        _gameRepository = gameRepository;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task HandleAsync(Guid gameId, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        Game game = await _gameRepository.GetByIdAsync(gameId, cancellationToken)
            ?? throw new GameNotFoundException(gameId.ToString());

        _gameRepository.Remove(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Files go after the rows: leftover files are harmless, missing ones are not.
        _mediaStorage.DeleteDirectory(GameMediaPaths.Directory(game.Id));

        _logger.LogInformation("Game {GameId} ({GameSlug}) deleted", game.Id, game.Slug.Value);
    }
}

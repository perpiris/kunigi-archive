using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Teams;
using KunigiArchive.Domain.Games;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Games.UpdateGameDetails;

// The host team's part of a game: title, note and story. The record (year, number, teams) is the admin's.
public sealed class UpdateGameDetailsHandler
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameQueries _gameQueries;
    private readonly ITeamQueries _teamQueries;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UpdateGameDetailsHandler> _logger;

    public UpdateGameDetailsHandler(
        IGameRepository gameRepository,
        IGameQueries gameQueries,
        ITeamQueries teamQueries,
        IRichTextSanitizer sanitizer,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<UpdateGameDetailsHandler> logger)
    {
        _gameRepository = gameRepository;
        _gameQueries = gameQueries;
        _teamQueries = teamQueries;
        _sanitizer = sanitizer;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<GameDetails> HandleAsync(UpdateGameDetailsCommand command, CancellationToken cancellationToken)
    {
        Game game = await _gameRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new GameNotFoundException(command.Id.ToString());

        await _currentUser.EnsureCanEditGameAsync(game.HostId, _teamQueries, cancellationToken);

        game.UpdateDetails(command.Title, command.Note, _sanitizer.Sanitize(command.Description));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Game {GameId} details updated", game.Id);

        return (await _gameQueries.GetDetailsAsync(game.Id, cancellationToken))!;
    }
}

public sealed record UpdateGameDetailsCommand(Guid Id, string? Title, string? Note, string? Description);

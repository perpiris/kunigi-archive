using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Games.CreateGame;

public sealed class CreateGameHandler
{
    private readonly IGameRepository _gameRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateGameHandler> _logger;

    public CreateGameHandler(
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateGameHandler> logger)
    {
        _gameRepository = gameRepository;
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<GameSummary> HandleAsync(CreateGameCommand command, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        Game game = Game.Create(command.Year, command.Order, command.HostId, command.WinnerId);
        await GameRecordRules.EnsureValidAsync(game, null, _gameRepository, _teamRepository, cancellationToken);

        _gameRepository.Add(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Game {GameId} created for {Year} with slug {GameSlug}", game.Id, game.Year, game.Slug.Value);

        return game.ToSummary();
    }
}
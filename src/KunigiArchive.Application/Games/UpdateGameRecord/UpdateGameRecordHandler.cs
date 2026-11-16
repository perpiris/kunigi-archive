using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Authorization;
using KunigiArchive.Application.Common;
using KunigiArchive.Domain.Common;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Games.UpdateGameRecord;

public sealed class UpdateGameRecordHandler
{
    private readonly IGameRepository _gameRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ISlugHistory _slugHistory;
    private readonly ILogger<UpdateGameRecordHandler> _logger;

    public UpdateGameRecordHandler(
        IGameRepository gameRepository,
        ITeamRepository teamRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ISlugHistory slugHistory,
        ILogger<UpdateGameRecordHandler> logger)
    {
        _gameRepository = gameRepository;
        _teamRepository = teamRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _slugHistory = slugHistory;
        _logger = logger;
    }

    public async Task<GameSummary> HandleAsync(UpdateGameRecordCommand command, CancellationToken cancellationToken)
    {
        _currentUser.EnsureAdmin();

        Game game = await _gameRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new GameNotFoundException(command.Id.ToString());

        Slug previousSlug = game.Slug;
        game.SetRecord(command.Year, command.Order, command.HostId, command.WinnerId);
        await GameRecordRules.EnsureValidAsync(game, game.Id, _gameRepository, _teamRepository, cancellationToken);

        // A new number means a new name and address (#23); the old address redirects to the new one.
        if (game.Slug != previousSlug)
        {
            await _slugHistory.RecordAsync(SlugOwner.Game, previousSlug.Value, game.Id, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Game {GameId} record updated: {Year}, slug {GameSlug}", game.Id, game.Year, game.Slug.Value);

        return game.ToSummary();
    }
}
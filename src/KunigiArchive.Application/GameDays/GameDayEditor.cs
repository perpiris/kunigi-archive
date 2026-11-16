using KunigiArchive.Application.Games;
using KunigiArchive.Domain.GameDays;

namespace KunigiArchive.Application.GameDays;

// Loads a day for a change, after checking the user may edit its game. Shared by the day and puzzle handlers.
public sealed class GameDayEditor
{
    private readonly IGameDayRepository _gameDayRepository;
    private readonly GameAccess _gameAccess;

    public GameDayEditor(IGameDayRepository gameDayRepository, GameAccess gameAccess)
    {
        _gameDayRepository = gameDayRepository;
        _gameAccess = gameAccess;
    }

    public async Task<GameDay> LoadAsync(Guid dayId, CancellationToken cancellationToken)
    {
        GameDay day = await _gameDayRepository.GetByIdAsync(dayId, cancellationToken)
            ?? throw new GameDayNotFoundException(dayId.ToString());

        await _gameAccess.EnsureCanEditAsync(day.GameId, cancellationToken);
        return day;
    }

    // A puzzle of another day is "not found" (404), not a bad request.
    public static Puzzle FindPuzzle(GameDay day, Guid puzzleId)
    {
        return day.Puzzles.FirstOrDefault(p => p.Id == puzzleId) ?? throw new PuzzleNotFoundException(puzzleId);
    }
}

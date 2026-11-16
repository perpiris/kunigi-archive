namespace KunigiArchive.Domain.GameDays;

public interface IGameDayRepository
{
    // The whole aggregate: the day's media and its puzzles with theirs.
    Task<GameDay?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GameDay?> GetAsync(Guid gameId, GameDayType type, CancellationToken cancellationToken);
    void Add(GameDay day);
}

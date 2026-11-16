using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Domain.GameDays;

namespace KunigiArchive.Application.GameDays;

public interface IGameDayQueries
{
    // The days the game has (enabled or not), in the order a game runs.
    Task<IReadOnlyList<GameDaySummary>> GetForGameAsync(Guid gameId, CancellationToken cancellationToken);

    Task<GameDayDetails?> GetAsync(Guid gameId, GameDayType type, CancellationToken cancellationToken);
}

public sealed class GameDayNotFoundException(string day)
    : NotFoundException($"Δεν βρέθηκε η ημέρα '{day}'.");

public sealed class PuzzleNotFoundException(Guid id)
    : NotFoundException($"Δεν βρέθηκε ο γρίφος '{id}'.");

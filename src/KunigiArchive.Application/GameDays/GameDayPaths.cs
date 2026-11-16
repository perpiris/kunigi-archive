using KunigiArchive.Application.Games;

namespace KunigiArchive.Application.GameDays;

public static class GameDayPaths
{
    // Inside the game's folder, so deleting the game takes its days' and puzzles' files too.
    public static string Directory(Guid gameId, Guid dayId)
    {
        return $"{GameMediaPaths.Directory(gameId)}/days/{dayId}";
    }

    public static string PuzzleDirectory(Guid gameId, Guid dayId, Guid puzzleId)
    {
        return $"{Directory(gameId, dayId)}/puzzles/{puzzleId}";
    }
}

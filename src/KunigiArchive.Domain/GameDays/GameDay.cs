using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.GameDays;

// One part of a game (the ball, Saturday, the online game...) with its puzzles and media. Its own aggregate,
// so editing a game never loads every puzzle, and the puzzle numbering rules live in one place.
// A day is created the first time it's enabled; disabling it hides it but keeps what was written.
public sealed class GameDay
{
    public const int TitleMaxLength = 200;

    public Guid Id { get; private set; }
    public Guid GameId { get; private set; }
    public GameDayType Type { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? Title { get; private set; }

    // Rich text (sanitized HTML).
    public string? Description { get; private set; }

    private readonly List<GameDayMedia> _media = [];
    public IReadOnlyCollection<GameDayMedia> Media => _media.AsReadOnly();

    private readonly List<Puzzle> _puzzles = [];
    public IReadOnlyCollection<Puzzle> Puzzles => _puzzles.AsReadOnly();

    private GameDay()
    {
    }

    public static GameDay Create(Guid gameId, GameDayType type)
    {
        if (!GameDayTypes.IsDefined(type))
        {
            throw new DomainException("Άγνωστο είδος ημέρας.");
        }

        return new GameDay { Id = Guid.CreateVersion7(), GameId = gameId, Type = type };
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    public void UpdateDetails(string? title, string? description)
    {
        string? trimmedTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (trimmedTitle?.Length > TitleMaxLength)
        {
            throw new DomainException($"Ο τίτλος δεν μπορεί να ξεπερνά τους {TitleMaxLength} χαρακτήρες.");
        }

        Title = trimmedTitle;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public GameDayMedia AddMedia(string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        GameDayMedia media = new GameDayMedia(Id, path, fileName, originalFileName, uploadedAt);
        _media.Add(media);
        return media;
    }

    public GameDayMedia RemoveMedia(Guid mediaId)
    {
        GameDayMedia media = _media.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new DomainException("Το αρχείο δεν ανήκει στην ημέρα.");
        _media.Remove(media);
        return media;
    }

    // ---------- Puzzles ----------
    // Numbered 1, 2, 3... in the order they were played. Puzzles with the same group number form a "package"
    // (handed out together); a group always has at least two puzzles.

    public Puzzle GetPuzzle(Guid puzzleId)
    {
        return _puzzles.FirstOrDefault(p => p.Id == puzzleId)
            ?? throw new DomainException("Ο γρίφος δεν ανήκει στην ημέρα.");
    }

    // A new puzzle goes last.
    public Puzzle AddPuzzle(int? group, string? question, string? answer)
    {
        int order = _puzzles.Count == 0 ? 1 : _puzzles.Max(p => p.Order) + 1;
        Puzzle puzzle = new Puzzle(Id, order);
        puzzle.Update(group, question, answer);
        _puzzles.Add(puzzle);
        return puzzle;
    }

    public void UpdatePuzzle(Guid puzzleId, int? group, string? question, string? answer)
    {
        Puzzle puzzle = GetPuzzle(puzzleId);
        int? previousGroup = puzzle.Group;
        puzzle.Update(group, question, answer);

        if (previousGroup != puzzle.Group)
        {
            DissolveIfAlone(previousGroup);
        }
    }

    // The puzzles after it move up one, so the numbers stay 1..n. A group left with one puzzle is dissolved.
    public Puzzle RemovePuzzle(Guid puzzleId)
    {
        Puzzle puzzle = GetPuzzle(puzzleId);
        _puzzles.Remove(puzzle);

        foreach (Puzzle later in _puzzles.Where(p => p.Order > puzzle.Order))
        {
            later.SetOrder(later.Order - 1);
        }

        DissolveIfAlone(puzzle.Group);
        return puzzle;
    }

    // Swaps the puzzle with the one before (up) or after it. At either end nothing changes.
    public void MovePuzzle(Guid puzzleId, bool up)
    {
        Puzzle puzzle = GetPuzzle(puzzleId);
        int neighbourOrder = up ? puzzle.Order - 1 : puzzle.Order + 1;
        Puzzle? neighbour = _puzzles.FirstOrDefault(p => p.Order == neighbourOrder);
        if (neighbour is null)
        {
            return;
        }

        neighbour.SetOrder(puzzle.Order);
        puzzle.SetOrder(neighbourOrder);
    }

    private void DissolveIfAlone(int? group)
    {
        if (group is null)
        {
            return;
        }

        List<Puzzle> members = _puzzles.Where(p => p.Group == group).ToList();
        if (members.Count == 1)
        {
            members[0].ClearGroup();
        }
    }
}

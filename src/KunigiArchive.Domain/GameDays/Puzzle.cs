using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.GameDays;

// Part of the GameDay aggregate: created, numbered and removed through its day.
public sealed class Puzzle
{
    public const int GroupMax = 999;

    public Guid Id { get; private set; }
    public Guid GameDayId { get; private set; }
    public int Order { get; private set; }
    public int? Group { get; private set; }

    // Rich text (sanitized HTML). Either may be empty: some puzzles are only a picture.
    public string? Question { get; private set; }
    public string? Answer { get; private set; }

    private readonly List<PuzzleMedia> _media = [];
    public IReadOnlyCollection<PuzzleMedia> Media => _media.AsReadOnly();

    private Puzzle()
    {
    }

    internal Puzzle(Guid gameDayId, int order)
    {
        Id = Guid.CreateVersion7();
        GameDayId = gameDayId;
        Order = order;
    }

    internal void Update(int? group, string? question, string? answer)
    {
        if (group is not null && (group < 1 || group > GroupMax))
        {
            throw new DomainException($"Ο αριθμός πακέτου πρέπει να είναι από 1 έως {GroupMax}.");
        }

        Group = group;
        Question = string.IsNullOrWhiteSpace(question) ? null : question.Trim();
        Answer = string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
    }

    internal void SetOrder(int order)
    {
        Order = order;
    }

    internal void ClearGroup()
    {
        Group = null;
    }

    public PuzzleMedia AddMedia(PuzzleMediaKind kind, string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("Το αρχείο πρέπει να ανήκει στην ερώτηση ή στην απάντηση.");
        }

        PuzzleMedia media = new PuzzleMedia(Id, kind, path, fileName, originalFileName, uploadedAt);
        _media.Add(media);
        return media;
    }

    public PuzzleMedia RemoveMedia(Guid mediaId)
    {
        PuzzleMedia media = _media.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new DomainException("Το αρχείο δεν ανήκει στον γρίφο.");
        _media.Remove(media);
        return media;
    }
}

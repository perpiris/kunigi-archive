using System.Globalization;
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Games;

// The game and its media. Its days (with their puzzles) and its posts are aggregates of their own.
public sealed class Game
{
    public const short YearMin = 1900;
    public const decimal OrderMax = 999.9m;
    public const int OrderTitleMaxLength = 100;
    public const int TitleMaxLength = 200;
    public const int NoteMaxLength = 300;
    public const int ImageMaxLength = 500;

    private const string BaseTitle = "Κυνήγι Θησαυρού";
    private static readonly CultureInfo Greek = CultureInfo.GetCultureInfo("el-GR");

    public Guid Id { get; private set; }
    public short Year { get; private set; }

    // Most games are numbered 1, 2, 3...; a few are not: 31,5 «Μισαδάκι» (the 2021 game during
    // covid) and the unnumbered 1988 «Το Ξεκίνημα». So the number is optional and allows a half.
    public decimal? Order { get; private set; }

    public Slug Slug { get; private set; } = null!;
    public string OrderTitle { get; private set; } = null!;
    public string? Title { get; private set; }

    // Anything the host and winner can't say: "Δεν αναδείχθηκε νικητής", "Ανεπίσημα", ...
    public string? Note { get; private set; }

    public string? Description { get; private set; }
    public string? Image { get; private set; }

    // Teams are their own aggregate, so a game only holds their ids. Both are optional: the
    // earliest games have no recorded host or winner team.
    public Guid? HostId { get; private set; }
    public Guid? WinnerId { get; private set; }

    private readonly List<GameMedia> _media = [];
    public IReadOnlyCollection<GameMedia> Media => _media.AsReadOnly();

    private Game()
    {
    }

    // The admin records which game it was: year, number, host and winner. The title, note,
    // description and pictures are the host team's managers' to fill in.
    public static Game Create(short year, decimal? order, Guid? hostId, Guid? winnerId)
    {
        Game game = new() { Id = Guid.CreateVersion7() };
        game.SetRecord(year, order, hostId, winnerId);
        return game;
    }

    /// <summary>Changes the year, number, host or winner. The name and the slug follow the number.</summary>
    public void SetRecord(short year, decimal? order, Guid? hostId, Guid? winnerId)
    {
        if (year < YearMin)
        {
            throw new DomainException($"Η χρονιά δεν μπορεί να είναι πριν το {YearMin}.");
        }

        ValidateOrder(order);

        Year = year;
        Order = order;
        OrderTitle = OrderTitleFor(year, order);
        // "31,5" would lose its comma and become "315"; a space keeps the parts apart.
        Slug = Slug.FromName(OrderTitle.Replace(',', ' '));
        HostId = hostId;
        WinnerId = winnerId;
    }

    /// <summary>The host team's part: the game's own title («Το Ξεκίνημα»), a short note and the story.</summary>
    public void UpdateDetails(string? title, string? note, string? description)
    {
        Title = Optional(title, TitleMaxLength, "Ο τίτλος");
        Note = Optional(note, NoteMaxLength, "Η σημείωση");
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    // Returns the previous poster so the caller can delete its file once the change is saved.
    public string? SetImage(string? path)
    {
        string? previous = Image;
        Image = path;
        return previous;
    }

    public GameMedia AddMedia(string path, string fileName, string originalFileName, DateTimeOffset uploadedAt)
    {
        GameMedia media = new GameMedia(Id, path, fileName, originalFileName, uploadedAt);
        _media.Add(media);
        return media;
    }

    public GameMedia RemoveMedia(Guid mediaId)
    {
        GameMedia media = _media.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new DomainException("Το αρχείο δεν ανήκει στο παιχνίδι.");
        _media.Remove(media);
        return media;
    }

    /// <summary>"34ο Κυνήγι Θησαυρού", "31,5ο Κυνήγι Θησαυρού", or "Κυνήγι Θησαυρού 1988" for an unnumbered game.</summary>
    public static string OrderTitleFor(short year, decimal? order)
    {
        if (order is null)
        {
            return $"{BaseTitle} {year}";
        }

        return order % 1 == 0
            ? $"{order.Value:0}ο {BaseTitle}"
            : $"{order.Value.ToString("0.#", Greek)}ο {BaseTitle}";
    }

    private static string? Optional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{label} δεν μπορεί να ξεπερνά τους {maxLength} χαρακτήρες.");
        }

        return trimmed;
    }

    private static void ValidateOrder(decimal? order)
    {
        if (order is null)
        {
            return;
        }

        if (order <= 0 || order > OrderMax)
        {
            throw new DomainException($"Ο αριθμός του παιχνιδιού πρέπει να είναι από 1 έως {OrderMax:0}.");
        }

        if (order * 10 % 1 != 0)
        {
            throw new DomainException("Ο αριθμός του παιχνιδιού μπορεί να έχει μόνο ένα δεκαδικό ψηφίο (π.χ. 31,5).");
        }
    }
}
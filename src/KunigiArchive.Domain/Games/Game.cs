using System.Globalization;
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Games;

// Only the game itself for now; days, puzzles, posts and media come with the game pages.
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
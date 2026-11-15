using KunigiArchive.Domain.Common;

namespace KunigiArchive.Domain.Games;

// Only the game itself for now; days, puzzles, posts and media come with the game pages.
public sealed class Game
{
    public const short YearMin = 1900;
    public const int OrderTitleMaxLength = 100;
    public const int TitleMaxLength = 200;
    public const int ImageMaxLength = 500;

    public Guid Id { get; private set; }
    public short Year { get; private set; }
    public short Order { get; private set; }
    public Slug Slug { get; private set; } = null!;
    public string OrderTitle { get; private set; } = null!;
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? Image { get; private set; }

    // Teams are their own aggregate, so a game only holds their ids.
    public Guid HostId { get; private set; }
    public Guid? WinnerId { get; private set; }

    private Game()
    {
    }

    public static Game Create(short year, short order, Guid hostId, Guid? winnerId)
    {
        if (year < YearMin)
        {
            throw new DomainException($"Η χρονιά δεν μπορεί να είναι πριν το {YearMin}.");
        }

        if (order < 1)
        {
            throw new DomainException("Η σειρά του παιχνιδιού πρέπει να είναι θετικός αριθμός.");
        }

        string orderTitle = OrderTitleFor(order);

        return new Game
        {
            Id = Guid.CreateVersion7(),
            Year = year,
            Order = order,
            OrderTitle = orderTitle,
            Slug = Slug.FromName(orderTitle),
            HostId = hostId,
            WinnerId = winnerId
        };
    }

    private static string OrderTitleFor(short order)
    {
        return $"{order}ο Κυνήγι Θησαυρού";
    }
}

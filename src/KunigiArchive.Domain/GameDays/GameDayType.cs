using System.Diagnostics.CodeAnalysis;

namespace KunigiArchive.Domain.GameDays;

// The parts a game can have. A fixed list (the old app seeded the same six as a table); stored as a number.
public enum GameDayType : byte
{
    Ball = 1,
    Saturday = 2,
    Sunday = 3,
    Online = 4,
    Kids = 5,
    Teens = 6
}

public static class GameDayTypes
{
    private static readonly Dictionary<GameDayType, string> Labels = new()
    {
        [GameDayType.Ball] = "Χορός",
        [GameDayType.Saturday] = "Σάββατο",
        [GameDayType.Sunday] = "Κυριακή",
        [GameDayType.Online] = "Διαδικτυακό",
        [GameDayType.Kids] = "Παιδικό",
        [GameDayType.Teens] = "Εφηβικό"
    };

    // "sabbato", "kuriaki", ...: the day's part of its address, /games/{game}/days/{day}.
    private static readonly Dictionary<GameDayType, string> Slugs =
        Labels.ToDictionary(pair => pair.Key, pair => Common.Slug.FromName(pair.Value).Value);

    // In the order a game runs.
    public static IReadOnlyList<GameDayType> All { get; } = Enum.GetValues<GameDayType>();

    public static string Label(this GameDayType type)
    {
        return Labels[type];
    }

    public static string Slug(this GameDayType type)
    {
        return Slugs[type];
    }

    public static bool IsDefined(GameDayType type)
    {
        return Labels.ContainsKey(type);
    }

    public static bool TryParseSlug(string? value, [NotNullWhen(true)] out GameDayType? type)
    {
        foreach ((GameDayType key, string slug) in Slugs)
        {
            if (string.Equals(slug, value, StringComparison.Ordinal))
            {
                type = key;
                return true;
            }
        }

        type = null;
        return false;
    }
}

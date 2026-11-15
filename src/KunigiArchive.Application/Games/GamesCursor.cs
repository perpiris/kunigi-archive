using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using KunigiArchive.Application.Common;

namespace KunigiArchive.Application.Games;

// Keyset position in the (Year descending, Id) order: the next page starts after this game.
public sealed record GamesCursor(short Year, Guid Id)
{
    public string Encode()
    {
        return KeysetCursor.Encode(Year.ToString(CultureInfo.InvariantCulture), Id);
    }

    public static bool TryDecode(string? value, [NotNullWhen(true)] out GamesCursor? cursor)
    {
        cursor = KeysetCursor.TryDecode(value, out string? key, out Guid id)
                 && short.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out short year)
            ? new GamesCursor(year, id)
            : null;
        return cursor is not null;
    }
}
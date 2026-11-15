using System.Diagnostics.CodeAnalysis;
using KunigiArchive.Application.Common;

namespace KunigiArchive.Application.Teams;

// Keyset position in the (Name, Id) order: the next page starts after this team.
public sealed record TeamsCursor(string Name, Guid Id)
{
    public string Encode()
    {
        return KeysetCursor.Encode(Name, Id);
    }

    public static bool TryDecode(string? value, [NotNullWhen(true)] out TeamsCursor? cursor)
    {
        cursor = KeysetCursor.TryDecode(value, out string? name, out Guid id) ? new TeamsCursor(name, id) : null;
        return cursor is not null;
    }
}

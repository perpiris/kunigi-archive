using System.Diagnostics.CodeAnalysis;
using KunigiArchive.Application.Common;

namespace KunigiArchive.Application.Users;

// Keyset position in the (Email, Id) order: the next page starts after this user.
public sealed record UsersCursor(string Email, Guid Id)
{
    public string Encode()
    {
        return KeysetCursor.Encode(Email, Id);
    }

    public static bool TryDecode(string? value, [NotNullWhen(true)] out UsersCursor? cursor)
    {
        cursor = KeysetCursor.TryDecode(value, out string? email, out Guid id) ? new UsersCursor(email, id) : null;
        return cursor is not null;
    }
}

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace KunigiArchive.Application.Common;

// Opaque "key|id" token for keyset paging over a (Key, Id) order.
public static class KeysetCursor
{
    public static string Encode(string key, Guid id)
    {
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{key}|{id}"));
    }

    public static bool TryDecode(string? value, [NotNullWhen(true)] out string? key, out Guid id)
    {
        key = null;
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
        }
        catch (FormatException)
        {
            return false;
        }

        // Split on the last separator: keys (team names) may contain '|'.
        int separator = raw.LastIndexOf('|');
        if (separator <= 0 || !Guid.TryParse(raw.AsSpan(separator + 1), out id))
        {
            return false;
        }

        key = raw[..separator];
        return true;
    }
}

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace KunigiArchive.Application.Teams;

// Keyset position in the (Name, Id) order: the next page starts after this team.
public sealed record TeamsCursor(string Name, Guid Id)
{
    public string Encode()
    {
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{Name}|{Id}"));
    }

    public static bool TryDecode(string? value, [NotNullWhen(true)] out TeamsCursor? cursor)
    {
        cursor = null;
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

        // Split on the last separator: team names may contain '|'.
        int separator = raw.LastIndexOf('|');
        if (separator <= 0 || !Guid.TryParse(raw.AsSpan(separator + 1), out Guid id))
        {
            return false;
        }

        cursor = new TeamsCursor(raw[..separator], id);
        return true;
    }
}
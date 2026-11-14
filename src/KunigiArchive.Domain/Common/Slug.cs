using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace KunigiArchive.Domain.Common;

public sealed record Slug
{
    public const int MaxLength = 200;

    private static readonly Dictionary<char, string> GreekToLatin = new()
    {
        ['α'] = "a",
        ['β'] = "b",
        ['γ'] = "g",
        ['δ'] = "d",
        ['ε'] = "e",
        ['ζ'] = "z",
        ['η'] = "i",
        ['θ'] = "th",
        ['ι'] = "i",
        ['κ'] = "k",
        ['λ'] = "l",
        ['μ'] = "m",
        ['ν'] = "n",
        ['ξ'] = "x",
        ['ο'] = "o",
        ['π'] = "p",
        ['ρ'] = "r",
        ['σ'] = "s",
        ['ς'] = "s",
        ['τ'] = "t",
        ['υ'] = "u",
        ['φ'] = "f",
        ['χ'] = "x",
        ['ψ'] = "ps",
        ['ω'] = "o",
        ['&'] = "kai"
    };

    // Slugs become media folder names, so Windows device names are refused. "create" and "manage"
    // are fixed routes under /teams, so a team with either slug could never be reached.
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "create", "manage",
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
    };

    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    public static Slug FromName(string name)
    {
        string value = Generate(name);
        if (value.Length == 0)
        {
            throw new DomainException($"Το '{name}' δεν μπορεί να γίνει διεύθυνση URL. Χρησιμοποιήστε γράμματα ή αριθμούς.");
        }

        if (value.Length > MaxLength)
        {
            throw new DomainException($"Η διεύθυνση URL για το '{name}' ξεπερνά τους {MaxLength} χαρακτήρες.");
        }

        return new Slug(value);
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out Slug? slug)
    {
        if (value is null || value.Length > MaxLength || value.Length == 0 || Generate(value) != value)
        {
            slug = null;
            return false;
        }

        slug = new Slug(value);
        return true;
    }

    public static Slug Parse(string value)
    {
        if (!TryParse(value, out Slug? slug))
        {
            throw new DomainException($"Το '{value}' δεν είναι έγκυρο slug.");
        }

        return slug;
    }

    public override string ToString()
    {
        return Value;
    }

    private static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string decomposed = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(decomposed.Length);

        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (GreekToLatin.TryGetValue(c, out string? latin))
            {
                builder.Append(latin);
            }
            else if (char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_')
            {
                builder.Append(c);
            }
            else if ((char.IsWhiteSpace(c) || c == '-') && builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        string slug = builder.ToString().TrimEnd('-');
        return ReservedNames.Contains(slug) ? string.Empty : slug;
    }
}
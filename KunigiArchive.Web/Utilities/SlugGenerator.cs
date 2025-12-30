using System.Globalization;
using System.Text;

namespace KunigiArchive.Web.Utilities;

public static class SlugGenerator
{
    private static readonly HashSet<char> ValidChars = ['-', '_'];

    private static readonly Dictionary<char, string> TransliterationMap = new()
    {
        { 'α', "a" },
        { 'ά', "a" },
        { 'Α', "a" },
        { 'Ά', "a" },
        { 'β', "b" },
        { 'Β', "b" },
        { 'γ', "g" },
        { 'Γ', "g" },
        { 'δ', "d" },
        { 'Δ', "d" },
        { 'ε', "e" },
        { 'έ', "e" },
        { 'Ε', "e" },
        { 'Έ', "e" },
        { 'ζ', "z" },
        { 'Ζ', "z" },
        { 'η', "i" },
        { 'ή', "i" },
        { 'Η', "i" },
        { 'Ή', "i" },
        { 'θ', "th" },
        { 'Θ', "th" },
        { 'ι', "i" },
        { 'ί', "i" },
        { 'ϊ', "i" },
        { 'Ι', "i" },
        { 'Ί', "i" },
        { 'Ϊ', "i" },
        { 'κ', "k" },
        { 'Κ', "k" },
        { 'λ', "l" },
        { 'Λ', "l" },
        { 'μ', "m" },
        { 'Μ', "m" },
        { 'ν', "n" },
        { 'Ν', "n" },
        { 'ξ', "x" },
        { 'Ξ', "x" },
        { 'ο', "o" },
        { 'ό', "o" },
        { 'Ο', "o" },
        { 'Ό', "o" },
        { 'π', "p" },
        { 'Π', "p" },
        { 'ρ', "r" },
        { 'Ρ', "r" },
        { 'σ', "s" },
        { 'ς', "s" },
        { 'Σ', "s" },
        { 'τ', "t" },
        { 'Τ', "t" },
        { 'υ', "u" },
        { 'ύ', "u" },
        { 'ϋ', "u" },
        { 'Υ', "u" },
        { 'Ύ', "u" },
        { 'Ϋ', "u" },
        { 'φ', "f" },
        { 'Φ', "f" },
        { 'χ', "x" },
        { 'Χ', "x" },
        { 'ψ', "ps" },
        { 'Ψ', "ps" },
        { 'ω', "o" },
        { 'ώ', "o" },
        { 'Ω', "o" },
        { 'Ώ', "o" },
        { '&', "kai" }
    };

    public static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        input = input.Trim().Replace(' ', '-');
        input = input.Replace(".", "", StringComparison.Ordinal);
        input = input.Normalize(NormalizationForm.FormD);
        input = TransliterateGreekToLatin(input);
        input = RemoveDiacritics(input).ToLowerInvariant();
        input = RemoveInvalidCharacters(input);
        input = input.Replace("--", "-", StringComparison.Ordinal);
        input = input.Trim('-');

        if (string.IsNullOrWhiteSpace(input) || IsReservedName(input))
        {
            return string.Empty;
        }

        return input.Trim();
    }

    private static string TransliterateGreekToLatin(string input)
    {
        var result = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (TransliterationMap.TryGetValue(c, out var value))
            {
                result.Append(value);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    private static string RemoveDiacritics(string input)
    {
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalizedString.Length);
        foreach (var c in normalizedString.Where(c =>
                     CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            stringBuilder.Append(c);
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string RemoveInvalidCharacters(string input)
    {
        // Remove any characters that aren't alphanumeric, hyphen, or underscore
        // This also prevents path separators (/, \) and other problematic chars
        return new string(input.Where(x => char.IsLetterOrDigit(x) || ValidChars.Contains(x)).ToArray());
    }

    private static bool IsReservedName(string name)
    {
        // Windows reserved names
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };
        return reserved.Contains(name);
    }
}
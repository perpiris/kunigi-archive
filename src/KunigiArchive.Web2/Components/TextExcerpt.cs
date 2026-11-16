using System.Net;
using System.Text.RegularExpressions;

namespace KunigiArchive.Web2.Components;

// The start of a rich-text field as plain text, for a table cell. The result is text (Razor encodes it).
public static partial class TextExcerpt
{
    public static string? FromHtml(string? html, int maxLength = 90)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        string text = WebUtility.HtmlDecode(Tags().Replace(html, " "));
        text = Spaces().Replace(text, " ").Trim();
        if (text.Length == 0)
        {
            return null;
        }

        return text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength).TrimEnd(), "…");
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Ganss.Xss;
using KunigiArchive.Application.Abstractions;

namespace KunigiArchive.Infrastructure.Html;

/// <summary>
/// The allowlist matches what the Quill editor produces with the site's toolbar: paragraphs, headings,
/// bold/italic/underline/strike, links, lists, quotes and code. No styles, colors or sizes, so content
/// follows the theme. Anything else is dropped but its text is kept.
/// </summary>
internal sealed partial class RichTextSanitizer : IRichTextSanitizer
{
    private static readonly string[] Tags =
    [
        "p", "br", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "s", "sub", "sup",
        "a", "ul", "ol", "li", "blockquote", "pre", "code"
    ];

    // Quill 2 writes every list as <ol> and marks bullets with data-list; nesting uses ql-indent-N.
    private static readonly string[] Classes = Enumerable.Range(1, 8).Select(level => $"ql-indent-{level}").ToArray();

    private static readonly HashSet<string> DroppedWithContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "template", "noscript", "iframe", "object", "embed", "svg", "math", "textarea", "select", "title"
    };

    private readonly HtmlSanitizer _sanitizer;

    public RichTextSanitizer()
    {
        _sanitizer = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(Tags, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(["href", "data-list", "class"], StringComparer.OrdinalIgnoreCase),
            AllowedClasses = new HashSet<string>(Classes, StringComparer.Ordinal),
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(["http", "https", "mailto"], StringComparer.OrdinalIgnoreCase),
            UriAttributes = new HashSet<string>(["href"], StringComparer.OrdinalIgnoreCase)
        })
        {
            // Unknown wrappers (span, div, font, h1 from old content...) lose the tag but keep the text.
            KeepChildNodes = true
        };

        // ...except for elements whose content is code or embedded documents, which go entirely.
        _sanitizer.RemovingTag += (_, e) =>
        {
            if (DroppedWithContent.Contains(e.Tag.LocalName))
            {
                e.Tag.TextContent = string.Empty;
            }
        };

        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is not IElement element)
            {
                return;
            }

            if (element.LocalName == "a")
            {
                // Links open outside the archive and don't hand it the opener or pass on ranking.
                element.SetAttribute("target", "_blank");
                element.SetAttribute("rel", "noopener noreferrer nofollow");
            }

            // data-list only carries Quill's list kind, and only on list items.
            string? list = element.GetAttribute("data-list");
            if (list is not null && (element.LocalName != "li" || list is not ("bullet" or "ordered")))
            {
                element.RemoveAttribute("data-list");
            }
        };
    }

    public string? Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        string input = html.Contains('<') ? html : FromPlainText(html);
        string clean = _sanitizer.Sanitize(input).Trim();

        // An emptied editor still sends "<p><br></p>".
        return HasText(clean) ? clean : null;
    }

    private static string FromPlainText(string text)
    {
        StringBuilder html = new();
        foreach (string paragraph in BlankLines().Split(text.Replace("\r\n", "\n").Trim()))
        {
            string encoded = WebUtility.HtmlEncode(paragraph.Trim()).Replace("\n", "<br>");
            html.Append("<p>").Append(encoded).Append("</p>");
        }

        return html.ToString();
    }

    private static bool HasText(string html)
    {
        return !string.IsNullOrWhiteSpace(WebUtility.HtmlDecode(Tag().Replace(html, string.Empty)));
    }

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLines();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tag();
}

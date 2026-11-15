using AngleSharp.Dom;
using Ganss.Xss;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KunigiArchive.Web.Utilities;

/// <summary>
/// Renders stored rich text (Quill HTML) through an allowlist (#12). Sanitizing at render time also
/// covers every row saved before this fix, so no data migration is needed. The allowlist matches the
/// old editor's toolbar: text formats, headers, lists, alignment, colors, sizes, links, images and
/// YouTube/Vimeo videos.
/// </summary>
public static class RichTextHtml
{
    private static readonly HashSet<string> VideoHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "www.youtube.com", "youtube.com", "www.youtube-nocookie.com", "youtube-nocookie.com", "player.vimeo.com"
    };

    private static readonly string[] ImageDataPrefixes =
        ["data:image/png;", "data:image/jpeg;", "data:image/jpg;", "data:image/gif;", "data:image/webp;"];

    // Quill formats: sizes, indents, alignment, direction, code blocks and video embeds.
    private static readonly string[] QuillClasses =
    [
        "ql-size-small", "ql-size-large", "ql-size-huge", "ql-align-center", "ql-align-right", "ql-align-justify",
        "ql-direction-rtl", "ql-syntax", "ql-video",
        .. Enumerable.Range(1, 8).Select(level => $"ql-indent-{level}")
    ];

    private static readonly HashSet<string> DroppedWithContent = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "template", "noscript", "object", "embed", "svg", "math", "textarea", "select", "title"
    };

    private static readonly HtmlSanitizer Sanitizer = Create();

    public static IHtmlContent RichText(this IHtmlHelper _, string? html)
    {
        return new HtmlString(Sanitize(html));
    }

    public static string Sanitize(string? html)
    {
        return string.IsNullOrWhiteSpace(html) ? string.Empty : Sanitizer.Sanitize(html);
    }

    private static HtmlSanitizer Create()
    {
        HtmlSanitizer sanitizer = new(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(
            [
                "p", "br", "h1", "h2", "h3", "h4", "h5", "h6", "strong", "b", "em", "i", "u", "s", "strike", "sub", "sup",
                "span", "a", "ul", "ol", "li", "blockquote", "pre", "code", "img", "iframe"
            ], StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(
            [
                "href", "src", "alt", "width", "height", "class", "style", "data-list", "frameborder", "allowfullscreen"
            ], StringComparer.OrdinalIgnoreCase),
            AllowedCssProperties = new HashSet<string>(
                ["color", "background-color", "text-align", "direction"], StringComparer.OrdinalIgnoreCase),
            AllowedSchemes = new HashSet<string>(["http", "https", "mailto", "data"], StringComparer.OrdinalIgnoreCase),
            UriAttributes = new HashSet<string>(["href", "src"], StringComparer.OrdinalIgnoreCase),
            AllowedClasses = new HashSet<string>(QuillClasses, StringComparer.Ordinal)
        })
        {
            KeepChildNodes = true
        };

        // Removed tags keep their text, except code and embedded documents, which go entirely.
        sanitizer.RemovingTag += (_, e) =>
        {
            if (DroppedWithContent.Contains(e.Tag.LocalName))
            {
                e.Tag.TextContent = string.Empty;
            }
        };

        sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is not IElement element)
            {
                return;
            }

            switch (element.LocalName)
            {
                case "a":
                    // data: links could open an HTML document.
                    if (element.GetAttribute("href")?.TrimStart().StartsWith("data:", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        element.RemoveAttribute("href");
                    }

                    element.SetAttribute("target", "_blank");
                    element.SetAttribute("rel", "noopener noreferrer nofollow");
                    break;

                case "img":
                    // Quill pastes images as data: URIs; only raster images, never SVG.
                    string src = element.GetAttribute("src")?.Trim() ?? string.Empty;
                    if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                        && !ImageDataPrefixes.Any(prefix => src.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        element.Remove();
                    }

                    break;

                case "iframe":
                    // Only Quill's video embeds from known players, sandboxed.
                    if (!Uri.TryCreate(element.GetAttribute("src"), UriKind.Absolute, out Uri? video)
                        || video.Scheme != Uri.UriSchemeHttps
                        || !VideoHosts.Contains(video.Host))
                    {
                        element.Remove();
                        break;
                    }

                    element.SetAttribute("sandbox", "allow-scripts allow-same-origin allow-presentation allow-popups");
                    element.SetAttribute("referrerpolicy", "strict-origin-when-cross-origin");
                    break;
            }
        };

        return sanitizer;
    }
}

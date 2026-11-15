namespace KunigiArchive.Application.Abstractions;

/// <summary>
/// Cleans rich text (the editor's HTML) down to an allowlist of formatting tags, so scripts, event
/// handlers and foreign markup never reach the database or a page.
/// </summary>
public interface IRichTextSanitizer
{
    /// <summary>
    /// Returns safe HTML, or null when nothing readable is left. Plain text (no tags) becomes paragraphs.
    /// </summary>
    string? Sanitize(string? html);
}

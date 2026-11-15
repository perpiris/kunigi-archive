using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Media;

public static class MediaRules
{
    public const long MaxFileBytes = 100L * 1024 * 1024;
    public const long MaxRequestBytes = 250L * 1024 * 1024;
    public const string ThumbnailPrefix = "thumb_";

    // Uploads are stored as WebP (GIFs as they are) and PDFs as an image of their first page,
    // so everything kept on disk is an image.
    public static readonly IReadOnlySet<string> AllowedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };

    public static string AcceptAttribute => string.Join(',', AllowedExtensions);

    public static void Validate(MediaUpload upload)
    {
        if (!AllowedExtensions.Contains(Path.GetExtension(upload.FileName)))
        {
            throw new InvalidRequestException(
                $"Το αρχείο '{upload.FileName}' δεν υποστηρίζεται. Επιτρέπονται εικόνες (JPG, PNG, GIF, WebP) και PDF.");
        }

        if (upload.Length == 0)
        {
            throw new InvalidRequestException($"Το αρχείο '{upload.FileName}' είναι κενό.");
        }

        if (upload.Length > MaxFileBytes)
        {
            throw new InvalidRequestException($"Το αρχείο '{upload.FileName}' ξεπερνά τα {MaxFileBytes / 1024 / 1024} MB.");
        }
    }

    public static string ThumbnailFor(string path)
    {
        int separator = path.LastIndexOf('/');
        return separator < 0
            ? ThumbnailPrefix + path
            : string.Concat(path.AsSpan(0, separator + 1), ThumbnailPrefix, path.AsSpan(separator + 1));
    }
}
using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Media;

public static class MediaRules
{
    public const long MaxFileBytes = 100L * 1024 * 1024;
    public const long MaxRequestBytes = 250L * 1024 * 1024;
    public const string ThumbnailPrefix = "thumb_";

    // Images are stored as WebP (animated GIFs as they are) and PDFs as an image of their first page.
    public static readonly IReadOnlySet<string> ImageExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };

    // Videos (puzzles of the 2024 game, for one) are kept as uploaded, after a check that the file really is MP4.
    public static readonly IReadOnlySet<string> VideoExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4" };

    // Galleries take images and videos; a poster or profile picture only images.
    public static string AcceptAttribute => string.Join(',', ImageExtensions.Concat(VideoExtensions));
    public static string ImageAcceptAttribute => string.Join(',', ImageExtensions);

    public static void Validate(MediaUpload upload)
    {
        string extension = Path.GetExtension(upload.FileName);
        if (!ImageExtensions.Contains(extension) && !VideoExtensions.Contains(extension))
        {
            throw new InvalidRequestException(
                $"Το αρχείο '{upload.FileName}' δεν υποστηρίζεται. Επιτρέπονται εικόνες (JPG, PNG, GIF, WebP), PDF και βίντεο MP4.");
        }

        ValidateSize(upload);
    }

    public static void ValidateImage(MediaUpload upload)
    {
        if (!ImageExtensions.Contains(Path.GetExtension(upload.FileName)))
        {
            throw new InvalidRequestException(
                $"Το αρχείο '{upload.FileName}' δεν υποστηρίζεται. Επιτρέπονται εικόνες (JPG, PNG, GIF, WebP) και PDF.");
        }

        ValidateSize(upload);
    }

    public static bool IsVideo(string path)
    {
        return VideoExtensions.Contains(Path.GetExtension(path));
    }

    // Videos have no thumbnail: pages show the video itself (its first frame).
    public static string ThumbnailFor(string path)
    {
        if (IsVideo(path))
        {
            return path;
        }

        int separator = path.LastIndexOf('/');
        return separator < 0
            ? ThumbnailPrefix + path
            : string.Concat(path.AsSpan(0, separator + 1), ThumbnailPrefix, path.AsSpan(separator + 1));
    }

    private static void ValidateSize(MediaUpload upload)
    {
        if (upload.Length == 0)
        {
            throw new InvalidRequestException($"Το αρχείο '{upload.FileName}' είναι κενό.");
        }

        if (upload.Length > MaxFileBytes)
        {
            throw new InvalidRequestException($"Το αρχείο '{upload.FileName}' ξεπερνά τα {MaxFileBytes / 1024 / 1024} MB.");
        }
    }
}

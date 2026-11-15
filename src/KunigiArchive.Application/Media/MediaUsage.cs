namespace KunigiArchive.Application.Media;

// Files counts what was uploaded (thumbnails left out); Bytes is everything on disk, thumbnails included.
public sealed record MediaUsage(int Files, long Bytes)
{
    public static readonly MediaUsage None = new(0, 0);
}
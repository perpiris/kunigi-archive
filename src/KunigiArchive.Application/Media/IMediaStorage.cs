namespace KunigiArchive.Application.Media;

public interface IMediaStorage
{
    // Throws InvalidRequestException when the content isn't one of the allowed formats.
    Task<StoredMedia> SaveAsync(string directory, MediaUpload upload, CancellationToken cancellationToken);

    // Missing files are ignored and failures are logged, never thrown: the database row is already gone by then.
    void Delete(string path);

    void DeleteDirectory(string directory);

    // Everything under the media root.
    MediaUsage GetTotalUsage();

    // Each folder directly under a directory (e.g. "teams" gives one entry per team folder), keyed by folder name.
    IReadOnlyDictionary<string, MediaUsage> GetUsageOfSubfolders(string directory);
}
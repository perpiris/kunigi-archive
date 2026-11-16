using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Media;

// The steps every gallery upload shares: check all files first, store them, add a row for each, save once,
// and delete what was stored if anything fails, so a failed upload leaves neither rows nor files behind.
public static class MediaUploads
{
    public const int OriginalNameMaxLength = 255;

    public static async Task<List<T>> SaveAllAsync<T>(
        IMediaStorage storage,
        IUnitOfWork unitOfWork,
        string directory,
        IReadOnlyList<MediaUpload> files,
        Func<StoredMedia, string, T> addRow,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            throw new InvalidRequestException("Δεν επιλέχθηκαν αρχεία.");
        }

        foreach (MediaUpload file in files)
        {
            MediaRules.Validate(file);
        }

        List<StoredMedia> stored = [];
        List<T> added = [];
        try
        {
            foreach (MediaUpload file in files)
            {
                StoredMedia media = await storage.SaveAsync(directory, file, cancellationToken);
                stored.Add(media);
                added.Add(addRow(media, OriginalName(file)));
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (StoredMedia media in stored)
            {
                storage.Delete(media.Path);
            }

            throw;
        }

        return added;
    }

    public static string OriginalName(MediaUpload file)
    {
        string name = Path.GetFileName(file.FileName);
        return name.Length <= OriginalNameMaxLength ? name : name[..OriginalNameMaxLength];
    }
}

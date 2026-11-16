using KunigiArchive.Application.Games;
using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.Posts;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.Posts.UploadPostMedia;

public sealed class UploadPostMediaHandler
{
    private readonly IPostRepository _postRepository;
    private readonly GameAccess _gameAccess;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UploadPostMediaHandler> _logger;

    public UploadPostMediaHandler(
        IPostRepository postRepository,
        GameAccess gameAccess,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<UploadPostMediaHandler> logger)
    {
        _postRepository = postRepository;
        _gameAccess = gameAccess;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PostMediaDetails>> HandleAsync(
        UploadPostMediaCommand command,
        CancellationToken cancellationToken)
    {
        Post post = await _postRepository.GetByIdAsync(command.PostId, cancellationToken)
            ?? throw new PostNotFoundException(command.PostId);

        await _gameAccess.EnsureCanEditAsync(post.GameId, cancellationToken);

        if (command.Files.Count == 0)
        {
            throw new InvalidRequestException("Δεν επιλέχθηκαν αρχεία.");
        }

        // Check every file before writing any, so one bad file doesn't leave the others half uploaded.
        foreach (MediaUpload file in command.Files)
        {
            MediaRules.Validate(file);
        }

        string directory = PostMediaPaths.Directory(post.GameId, post.Id);
        List<StoredMedia> stored = [];
        List<PostMedia> added = [];
        try
        {
            foreach (MediaUpload file in command.Files)
            {
                StoredMedia media = await _mediaStorage.SaveAsync(directory, file, cancellationToken);
                stored.Add(media);
                added.Add(post.AddMedia(media.Path, media.FileName, OriginalName(file), _timeProvider.GetUtcNow()));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (StoredMedia media in stored)
            {
                _mediaStorage.Delete(media.Path);
            }

            throw;
        }

        _logger.LogInformation("{Count} media files uploaded to post {PostId}", added.Count, post.Id);

        return added.Select(m => m.ToDetails()).ToList();
    }

    private static string OriginalName(MediaUpload file)
    {
        string name = Path.GetFileName(file.FileName);
        return name.Length <= PostMedia.FileNameMaxLength ? name : name[..PostMedia.FileNameMaxLength];
    }
}

public sealed record UploadPostMediaCommand(Guid PostId, IReadOnlyList<MediaUpload> Files);
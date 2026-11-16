using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.GameDays;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.GameDays.ManageGameDay;

// Shows or hides a day on the game page. The first time a day is enabled, it's created.
public sealed class SetGameDayEnabledHandler
{
    private readonly IGameDayRepository _gameDayRepository;
    private readonly GameAccess _gameAccess;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetGameDayEnabledHandler> _logger;

    public SetGameDayEnabledHandler(
        IGameDayRepository gameDayRepository,
        GameAccess gameAccess,
        IUnitOfWork unitOfWork,
        ILogger<SetGameDayEnabledHandler> logger)
    {
        _gameDayRepository = gameDayRepository;
        _gameAccess = gameAccess;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameDaySummary> HandleAsync(SetGameDayEnabledCommand command, CancellationToken cancellationToken)
    {
        if (!GameDayTypes.IsDefined(command.Type))
        {
            throw new InvalidRequestException("Άγνωστο είδος ημέρας.");
        }

        await _gameAccess.EnsureCanEditAsync(command.GameId, cancellationToken);

        GameDay? day = await _gameDayRepository.GetAsync(command.GameId, command.Type, cancellationToken);
        if (day is null)
        {
            if (!command.Enabled)
            {
                throw new GameDayNotFoundException(command.Type.Slug());
            }

            day = GameDay.Create(command.GameId, command.Type);
            _gameDayRepository.Add(day);
        }

        day.SetEnabled(command.Enabled);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Day {DayType} of game {GameId} {State}", day.Type, day.GameId, command.Enabled ? "enabled" : "disabled");

        return day.ToSummary();
    }
}

public sealed record SetGameDayEnabledCommand(Guid GameId, GameDayType Type, bool Enabled);

public sealed class UpdateGameDayHandler
{
    private readonly GameDayEditor _editor;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateGameDayHandler> _logger;

    public UpdateGameDayHandler(
        GameDayEditor editor,
        IRichTextSanitizer sanitizer,
        IUnitOfWork unitOfWork,
        ILogger<UpdateGameDayHandler> logger)
    {
        _editor = editor;
        _sanitizer = sanitizer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GameDayDetails> HandleAsync(UpdateGameDayCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);

        day.UpdateDetails(command.Title, _sanitizer.Sanitize(command.Description));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Day {DayId} updated", day.Id);

        return day.ToDetails();
    }
}

public sealed record UpdateGameDayCommand(Guid DayId, string? Title, string? Description);

public sealed class UploadGameDayMediaHandler
{
    private readonly GameDayEditor _editor;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UploadGameDayMediaHandler> _logger;

    public UploadGameDayMediaHandler(
        GameDayEditor editor,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<UploadGameDayMediaHandler> logger)
    {
        _editor = editor;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediaDetails>> HandleAsync(UploadGameDayMediaCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);

        List<GameDayMedia> added = await MediaUploads.SaveAllAsync(
            _mediaStorage,
            _unitOfWork,
            GameDayPaths.Directory(day.GameId, day.Id),
            command.Files,
            (media, originalName) => day.AddMedia(media.Path, media.FileName, originalName, _timeProvider.GetUtcNow()),
            cancellationToken);

        _logger.LogInformation("{Count} media files uploaded to day {DayId}", added.Count, day.Id);

        return added.Select(m => m.ToDetails()).ToList();
    }
}

public sealed record UploadGameDayMediaCommand(Guid DayId, IReadOnlyList<MediaUpload> Files);

public sealed class DeleteGameDayMediaHandler
{
    private readonly GameDayEditor _editor;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteGameDayMediaHandler> _logger;

    public DeleteGameDayMediaHandler(
        GameDayEditor editor,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        ILogger<DeleteGameDayMediaHandler> logger)
    {
        _editor = editor;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteGameDayMediaCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);

        GameDayMedia media = day.RemoveMedia(command.MediaId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The row goes first: a leftover file is harmless, a row pointing at a deleted file is a broken image.
        _mediaStorage.Delete(media.Path);

        _logger.LogInformation("Media {MediaId} deleted from day {DayId}", media.Id, day.Id);
    }
}

public sealed record DeleteGameDayMediaCommand(Guid DayId, Guid MediaId);

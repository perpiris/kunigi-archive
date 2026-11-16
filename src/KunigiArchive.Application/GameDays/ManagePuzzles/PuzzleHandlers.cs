using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Mappers;
using KunigiArchive.Application.Media;
using KunigiArchive.Domain.GameDays;
using Microsoft.Extensions.Logging;

namespace KunigiArchive.Application.GameDays.ManagePuzzles;

// A new puzzle goes last. Its pictures are added afterwards, like a post's.
public sealed class CreatePuzzleHandler
{
    private readonly GameDayEditor _editor;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePuzzleHandler> _logger;

    public CreatePuzzleHandler(GameDayEditor editor, IRichTextSanitizer sanitizer, IUnitOfWork unitOfWork, ILogger<CreatePuzzleHandler> logger)
    {
        _editor = editor;
        _sanitizer = sanitizer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PuzzleDetails> HandleAsync(PuzzleCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);

        Puzzle puzzle = day.AddPuzzle(command.Group, _sanitizer.Sanitize(command.Question), _sanitizer.Sanitize(command.Answer));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Puzzle {PuzzleId} ({Order}) created in day {DayId}", puzzle.Id, puzzle.Order, day.Id);

        return puzzle.ToDetails();
    }
}

// For both creating (PuzzleId empty) and editing a puzzle.
public sealed record PuzzleCommand(Guid DayId, Guid PuzzleId, int? Group, string? Question, string? Answer);

public sealed class UpdatePuzzleHandler
{
    private readonly GameDayEditor _editor;
    private readonly IRichTextSanitizer _sanitizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePuzzleHandler> _logger;

    public UpdatePuzzleHandler(GameDayEditor editor, IRichTextSanitizer sanitizer, IUnitOfWork unitOfWork, ILogger<UpdatePuzzleHandler> logger)
    {
        _editor = editor;
        _sanitizer = sanitizer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PuzzleDetails> HandleAsync(PuzzleCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);
        Puzzle puzzle = GameDayEditor.FindPuzzle(day, command.PuzzleId);

        day.UpdatePuzzle(puzzle.Id, command.Group, _sanitizer.Sanitize(command.Question), _sanitizer.Sanitize(command.Answer));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Puzzle {PuzzleId} updated", puzzle.Id);

        return puzzle.ToDetails();
    }
}

public sealed class DeletePuzzleHandler
{
    private readonly GameDayEditor _editor;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePuzzleHandler> _logger;

    public DeletePuzzleHandler(GameDayEditor editor, IMediaStorage mediaStorage, IUnitOfWork unitOfWork, ILogger<DeletePuzzleHandler> logger)
    {
        _editor = editor;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // The puzzles after it move up one; a package left with one puzzle is dissolved.
    public async Task HandleAsync(PuzzleRef command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);
        Puzzle puzzle = GameDayEditor.FindPuzzle(day, command.PuzzleId);

        day.RemovePuzzle(puzzle.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Media rows go with the puzzle (cascade); the files after.
        _mediaStorage.DeleteDirectory(GameDayPaths.PuzzleDirectory(day.GameId, day.Id, puzzle.Id));

        _logger.LogInformation("Puzzle {PuzzleId} deleted from day {DayId}", puzzle.Id, day.Id);
    }
}

public sealed record PuzzleRef(Guid DayId, Guid PuzzleId);

// Swaps the puzzle with the one before or after it.
public sealed class MovePuzzleHandler
{
    private readonly GameDayEditor _editor;
    private readonly IUnitOfWork _unitOfWork;

    public MovePuzzleHandler(GameDayEditor editor, IUnitOfWork unitOfWork)
    {
        _editor = editor;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(MovePuzzleCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);
        Puzzle puzzle = GameDayEditor.FindPuzzle(day, command.PuzzleId);

        day.MovePuzzle(puzzle.Id, command.Up);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record MovePuzzleCommand(Guid DayId, Guid PuzzleId, bool Up);

public sealed class UploadPuzzleMediaHandler
{
    private readonly GameDayEditor _editor;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<UploadPuzzleMediaHandler> _logger;

    public UploadPuzzleMediaHandler(
        GameDayEditor editor,
        IMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<UploadPuzzleMediaHandler> logger)
    {
        _editor = editor;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediaDetails>> HandleAsync(UploadPuzzleMediaCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);
        Puzzle puzzle = GameDayEditor.FindPuzzle(day, command.PuzzleId);

        List<PuzzleMedia> added = await MediaUploads.SaveAllAsync(
            _mediaStorage,
            _unitOfWork,
            GameDayPaths.PuzzleDirectory(day.GameId, day.Id, puzzle.Id),
            command.Files,
            (media, originalName) => puzzle.AddMedia(command.Kind, media.Path, media.FileName, originalName, _timeProvider.GetUtcNow()),
            cancellationToken);

        _logger.LogInformation("{Count} {Kind} media files uploaded to puzzle {PuzzleId}", added.Count, command.Kind, puzzle.Id);

        return added.Select(m => m.ToDetails()).ToList();
    }
}

public sealed record UploadPuzzleMediaCommand(Guid DayId, Guid PuzzleId, PuzzleMediaKind Kind, IReadOnlyList<MediaUpload> Files);

public sealed class DeletePuzzleMediaHandler
{
    private readonly GameDayEditor _editor;
    private readonly IMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePuzzleMediaHandler> _logger;

    public DeletePuzzleMediaHandler(GameDayEditor editor, IMediaStorage mediaStorage, IUnitOfWork unitOfWork, ILogger<DeletePuzzleMediaHandler> logger)
    {
        _editor = editor;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(DeletePuzzleMediaCommand command, CancellationToken cancellationToken)
    {
        GameDay day = await _editor.LoadAsync(command.DayId, cancellationToken);
        Puzzle puzzle = GameDayEditor.FindPuzzle(day, command.PuzzleId);

        PuzzleMedia media = puzzle.RemoveMedia(command.MediaId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _mediaStorage.Delete(media.Path);

        _logger.LogInformation("Media {MediaId} deleted from puzzle {PuzzleId}", media.Id, puzzle.Id);
    }
}

public sealed record DeletePuzzleMediaCommand(Guid DayId, Guid PuzzleId, Guid MediaId);

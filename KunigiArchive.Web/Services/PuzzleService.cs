using System.Security.Claims;
using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Game;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Web.Services;

public class PuzzleService : IPuzzleService
{
    private readonly DataContext _context;
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<PuzzleService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!;

    public PuzzleService(
        DataContext context,
        IDirectoryService directoryService,
        ILogger<PuzzleService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _directoryService = directoryService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResponse<PuzzleManagementViewModel>> GetPuzzleManagementAsync(string gameSlug,
        long gameDayId)
    {
        var gameDay = await LoadGameDayWithAccess(gameDayId);
        if (gameDay == null)
        {
            return ServiceResponse<PuzzleManagementViewModel>.Failure("Η ημέρα δεν βρέθηκε.");
        }

        if (!HasGameAccess(gameDay.Game))
        {
            return ServiceResponse<PuzzleManagementViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var puzzles = await _context.Puzzles
            .Where(p => p.GameDayId == gameDayId)
            .Include(p => p.MediaFiles)
            .OrderBy(p => p.Order)
            .ToListAsync();

        var viewModel = new PuzzleManagementViewModel
        {
            GameDayId = gameDayId,
            GameSlug = gameSlug,
            GameDayName = gameDay.GameDayType.Label,
            GameFormattedTitle = FormatGameTitle(gameDay.Game),
            Puzzles = puzzles.Select(MapToPuzzleDetails).ToList()
        };

        return ServiceResponse<PuzzleManagementViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse<PuzzleCreateViewModel>> PrepareCreateViewModelAsync(string gameSlug,
        long gameDayId)
    {
        var gameDay = await LoadGameDayWithAccess(gameDayId);
        if (gameDay == null)
        {
            return ServiceResponse<PuzzleCreateViewModel>.Failure("Η ημέρα δεν βρέθηκε.");
        }

        if (!HasGameAccess(gameDay.Game))
        {
            return ServiceResponse<PuzzleCreateViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        return ServiceResponse<PuzzleCreateViewModel>.Successful(new PuzzleCreateViewModel { GameDayId = gameDayId });
    }

    public async Task<ServiceResponse> CreatePuzzleAsync(PuzzleCreateViewModel viewModel,
        ModelStateDictionary modelState)
    {
        var gameDay = await LoadGameDayWithAccess(viewModel.GameDayId);
        if (gameDay == null)
        {
            return ServiceResponse.Failure("Η ημέρα δεν βρέθηκε.");
        }

        if (!HasGameAccess(gameDay.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var maxOrder = await _context.Puzzles
            .Where(p => p.GameDayId == viewModel.GameDayId)
            .MaxAsync(p => (short?)p.Order) ?? 0;

        var puzzle = new Puzzle
        {
            GameDayId = viewModel.GameDayId,
            Order = (short)(maxOrder + 1),
            Group = viewModel.Group is > 0 ? viewModel.Group : null,
            Question = viewModel.Question,
            Answer = viewModel.Answer
        };

        _context.Puzzles.Add(puzzle);
        await _context.SaveChangesAsync();

        var storagePath = $"games/{gameDay.Game.Year}";
        await SavePuzzleMediaAsync(puzzle.PuzzleId, storagePath, viewModel.QuestionMediaFiles, "Q");
        await SavePuzzleMediaAsync(puzzle.PuzzleId, storagePath, viewModel.AnswerMediaFiles, "A");

        return ServiceResponse.Successful("Ο γρίφος δημιουργήθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse<PuzzleEditViewModel>> GetPuzzleEditAsync(long puzzleId)
    {
        var puzzle = await _context.Puzzles
            .Include(p => p.MediaFiles)
            .Include(p => p.GameDay)
            .ThenInclude(gd => gd.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(p => p.PuzzleId == puzzleId);

        if (puzzle == null)
        {
            return ServiceResponse<PuzzleEditViewModel>.Failure("Ο γρίφος δεν βρέθηκε.");
        }

        if (!HasGameAccess(puzzle.GameDay.Game))
        {
            return ServiceResponse<PuzzleEditViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var viewModel = new PuzzleEditViewModel
        {
            PuzzleId = puzzle.PuzzleId,
            GameDayId = puzzle.GameDayId,
            Question = puzzle.Question,
            Answer = puzzle.Answer,
            Group = puzzle.Group,
            QuestionMedia = puzzle.MediaFiles
                .Where(m => m.MediaType == "Q")
                .Select(m => new MediaFileViewModel
                {
                    MediaFileId = m.PuzzleMediaId, FileName = m.FileName, Path = m.Path
                }).ToList(),
            AnswerMedia = puzzle.MediaFiles
                .Where(m => m.MediaType == "A")
                .Select(m => new MediaFileViewModel
                {
                    MediaFileId = m.PuzzleMediaId, FileName = m.FileName, Path = m.Path
                }).ToList()
        };

        return ServiceResponse<PuzzleEditViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> UpdatePuzzleAsync(PuzzleEditViewModel viewModel,
        ModelStateDictionary modelState)
    {
        var puzzle = await _context.Puzzles
            .Include(p => p.GameDay)
            .ThenInclude(gd => gd.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(p => p.PuzzleId == viewModel.PuzzleId);

        if (puzzle == null)
        {
            return ServiceResponse.Failure("Ο γρίφος δεν βρέθηκε.");
        }

        if (!HasGameAccess(puzzle.GameDay.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        puzzle.Question = viewModel.Question;
        puzzle.Answer = viewModel.Answer;
        puzzle.Group = viewModel.Group is > 0 ? viewModel.Group : null;

        await _context.SaveChangesAsync();

        var storagePath = $"games/{puzzle.GameDay.Game.Year}";
        await SavePuzzleMediaAsync(puzzle.PuzzleId, storagePath, viewModel.QuestionMediaFiles, "Q");
        await SavePuzzleMediaAsync(puzzle.PuzzleId, storagePath, viewModel.AnswerMediaFiles, "A");

        return ServiceResponse.Successful("Ο γρίφος ενημερώθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse> DeletePuzzleAsync(long puzzleId)
    {
        var puzzle = await _context.Puzzles
            .Include(p => p.MediaFiles)
            .Include(p => p.GameDay)
            .ThenInclude(gd => gd.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(p => p.PuzzleId == puzzleId);

        if (puzzle == null)
        {
            return ServiceResponse.Failure("Ο γρίφος δεν βρέθηκε.");
        }

        if (!HasGameAccess(puzzle.GameDay.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        foreach (var media in puzzle.MediaFiles)
        {
            _directoryService.DeleteFile(media.Path);
        }

        var deletedOrder = puzzle.Order;
        var deletedGroup = puzzle.Group;
        var gameDayId = puzzle.GameDayId;

        _context.Puzzles.Remove(puzzle);
        await _context.SaveChangesAsync();

        // Re-order remaining puzzles
        var puzzlesToReorder = await _context.Puzzles
            .Where(p => p.GameDayId == gameDayId && p.Order > deletedOrder)
            .OrderBy(p => p.Order)
            .ToListAsync();

        foreach (var p in puzzlesToReorder)
        {
            p.Order--;
        }

        // Dissolve single-member groups
        if (deletedGroup.HasValue)
        {
            var remainingInGroup = await _context.Puzzles
                .Where(p => p.GameDayId == gameDayId && p.Group == deletedGroup)
                .ToListAsync();

            if (remainingInGroup.Count == 1)
            {
                remainingInGroup[0].Group = null;
            }
        }

        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Ο γρίφος διαγράφηκε επιτυχώς.");
    }

    public async Task<ServiceResponse> DeletePuzzleMediaAsync(long puzzleId, long puzzleMediaId)
    {
        var puzzle = await _context.Puzzles
            .Include(p => p.GameDay)
            .ThenInclude(gd => gd.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .Include(p => p.MediaFiles)
            .FirstOrDefaultAsync(p => p.PuzzleId == puzzleId);

        if (puzzle == null)
        {
            return ServiceResponse.Failure("Ο γρίφος δεν βρέθηκε.");
        }

        if (!HasGameAccess(puzzle.GameDay.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var media = puzzle.MediaFiles.FirstOrDefault(x => x.PuzzleMediaId == puzzleMediaId);
        if (media == null)
        {
            return ServiceResponse.Failure("Το αρχείο πολυμέσων δεν βρέθηκε.");
        }

        var deleteResult = _directoryService.DeleteFile(media.Path);
        if (!deleteResult.Success)
        {
            _logger.LogError("Failed to delete physical file {FilePath} for media ID {MediaId}: {Message}", media.Path,
                media.PuzzleMediaId, deleteResult.Message);
            return ServiceResponse.Failure("Σφάλμα κατά τη διαγραφή του αρχείου από το δίσκο.");
        }

        _context.PuzzleMedia.Remove(media);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Το αρχείο πολυμέσων διαγράφηκε με επιτυχία.");
    }

    private async Task<GameDay?> LoadGameDayWithAccess(long gameDayId)
    {
        return await _context.GameDays
            .Include(gd => gd.GameDayType)
            .Include(gd => gd.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(gd => gd.GameDayId == gameDayId);
    }

    private bool HasGameAccess(Game game)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return game.Host.Managers.Any(m => m.AppUserId == userId);
    }

    private async Task SavePuzzleMediaAsync(long puzzleId, string storagePath, List<IFormFile> files, string mediaType)
    {
        foreach (var file in files)
        {
            var saveResult = await _directoryService.SaveFileAsync(file, storagePath);
            if (!saveResult.Success || saveResult.Data == null)
            {
                _logger.LogWarning("Failed to save puzzle media file: {FileName}", file.FileName);
                continue;
            }

            var puzzleMedia = new PuzzleMedia
            {
                PuzzleId = puzzleId,
                MediaType = mediaType,
                Path = saveResult.Data,
                OriginalFileName = file.FileName,
                FileName = Path.GetFileName(saveResult.Data)
            };

            _context.PuzzleMedia.Add(puzzleMedia);
        }

        if (files.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private static PuzzleDetailsViewModel MapToPuzzleDetails(Puzzle puzzle)
    {
        return new PuzzleDetailsViewModel
        {
            PuzzleId = puzzle.PuzzleId,
            GameDayId = puzzle.GameDayId,
            Order = puzzle.Order,
            Group = puzzle.Group,
            Question = puzzle.Question,
            Answer = puzzle.Answer,
            QuestionMedia = puzzle.MediaFiles
                .Where(m => m.MediaType == "Q")
                .Select(m => new MediaFileViewModel
                {
                    MediaFileId = m.PuzzleMediaId, FileName = m.FileName, Path = m.Path
                }).ToList(),
            AnswerMedia = puzzle.MediaFiles
                .Where(m => m.MediaType == "A")
                .Select(m => new MediaFileViewModel
                {
                    MediaFileId = m.PuzzleMediaId, FileName = m.FileName, Path = m.Path
                }).ToList()
        };
    }

    private static string FormatGameTitle(Game game)
    {
        var result = $"{game.OrderTitle} {game.Year}";
        if (!string.IsNullOrWhiteSpace(game.Title))
        {
            result += $" - {game.Title}";
        }

        return result;
    }
}
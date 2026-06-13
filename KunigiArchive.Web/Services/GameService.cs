using System.Security.Claims;
using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Mappings;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.Utilities;
using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Game;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Web.Services;

public class GameService : IGameService
{
    private readonly DataContext _context;
    private readonly IDirectoryService _directoryService;
    private readonly ILogger<GameService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!;

    public GameService(
        DataContext context,
        IDirectoryService directoryService,
        ILogger<GameService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _directoryService = directoryService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResponse<PaginatedViewModel<GameDetailsViewModel>>> GetPaginatedGamesAsync(
        short pageNumber = 1, short pageSize = 8, string? searchQuery = null)
    {
        var query = _context.Games.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchLower = searchQuery.ToLower();
            query = query.Where(x =>
                x.OrderTitle.ToLower().Contains(searchLower) ||
                (x.Title != null && x.Title.ToLower().Contains(searchLower)) ||
                x.Year.ToString().Contains(searchLower));
        }

        query = query.OrderBy(x => x.Year);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var paginatedData = new PaginatedViewModel<GameDetailsViewModel>
        {
            Items = items.Select(x => x.MapToGameDetailsViewModel()).ToList(),
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            PageSize = pageSize
        };

        return ServiceResponse<PaginatedViewModel<GameDetailsViewModel>>.Successful(paginatedData);
    }

    public async Task<ServiceResponse<GameCreateViewModel>> PrepareGameCreateViewModelViewModel(
        GameCreateViewModel? gameCreateViewModel = null)
    {
        var teamList = await _context.Teams.ToListAsync();

        var selectListItems = teamList
            .Select(x => new SelectListItem { Value = x.TeamId.ToString(), Text = x.Name })
            .OrderBy(χ => χ.Text)
            .ToList();

        selectListItems.Insert(0, new SelectListItem { Value = "", Text = "Επιλογή Ομάδας" });
        var selectList = new SelectList(selectListItems, "Value", "Text");

        if (gameCreateViewModel == null)
        {
            gameCreateViewModel = new GameCreateViewModel
            {
                HostSelectList = selectList,
                WinnerSelectList = selectList,
                Year = (short)DateTime.Now.Year,
                Order = (short)(DateTime.Now.Year - 1990)
            };

            return ServiceResponse<GameCreateViewModel>.Successful(gameCreateViewModel);
        }

        gameCreateViewModel.HostSelectList = selectList;
        gameCreateViewModel.WinnerSelectList = selectList;

        return ServiceResponse<GameCreateViewModel>.Successful(gameCreateViewModel);
    }

    public async Task<ServiceResponse> CreateGameAsync(GameCreateViewModel gameCreateViewModel,
        ModelStateDictionary modelState)
    {
        var allGames = await _context.Games.ToListAsync();

        if (allGames.Any(x => x.Year == gameCreateViewModel.Year))
        {
            modelState.AddModelError("Year", "Υπάρχει ήδη παιχνίδι για αυτή τη χρονιά.");
        }

        if (allGames.Any(x => x.Order == gameCreateViewModel.Order))
        {
            modelState.AddModelError("Order", "Υπάρχει ήδη παιχνίδι για αυτή τη σειρά.");
        }

        if (!modelState.IsValid)
        {
            return ServiceResponse.Failure();
        }

        var newGame = new Game
        {
            Slug = SlugGenerator.GenerateSlug($"{gameCreateViewModel.Order}ο Κυνήγι Θησαυρού"),
            Year = gameCreateViewModel.Year,
            Order = gameCreateViewModel.Order,
            OrderTitle = $"{gameCreateViewModel.Order}ο Κυνήγι Θησαυρού",
            WinnerId = gameCreateViewModel.WinnerId,
            HostId = gameCreateViewModel.HostId
        };

        var gameDayTypes = await _context.GameDayTypes.ToListAsync();
        foreach (var gameDayType in gameDayTypes)
        {
            newGame.GameDays.Add(new GameDay { GameDayType = gameDayType });
        }

        _context.Games.Add(newGame);
        await _context.SaveChangesAsync();

        var directoryServiceResponse = _directoryService.CreateDirectory($"games/{gameCreateViewModel.Year}");
        return !directoryServiceResponse.Success
            ? ServiceResponse.Warning("Το παιχνίδι δημιουργήθηκε με επιτυχία άλλα η δημιουργία του φακέλου απέτυχε.")
            : ServiceResponse.Successful("Το παιχνίδι δημιουργήθηκε με επιτυχία.");
    }

    public async Task<ServiceResponse<GameDetailsViewModel>> GetGameBySlugAsync(string slug, bool includeFullDetails,
        bool isForPublicView = false)
    {
        var query = _context.Games.AsQueryable();
        if (includeFullDetails)
        {
            query = query
                .Include(x => x.Winner)
                .Include(x => x.GameDays)
                .ThenInclude(x => x.GameDayType)
                .Include(x => x.MediaFiles)
                .Include(x => x.Posts)
                .ThenInclude(p => p.MediaFiles)
                .AsSplitQuery();
        }

        query = query
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers);

        var game = await query.FirstOrDefaultAsync(x => x.Slug == slug);
        if (game == null)
        {
            return ServiceResponse<GameDetailsViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!isForPublicView && !HasGameAccess(game))
        {
            return ServiceResponse<GameDetailsViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        return ServiceResponse<GameDetailsViewModel>.Successful(game.MapToGameDetailsViewModel(includeFullDetails));
    }

    public async Task<ServiceResponse<GameDayDetailsViewModel>> GetGameDayDetailsForPublicViewAsync(string slug,
        long gameDayId)
    {
        var game = await _context.Games
            .AsNoTracking()
            .Include(x => x.GameDays.Where(gd => gd.GameDayId == gameDayId))
            .ThenInclude(gd => gd.GameDayType)
            .Include(x => x.GameDays.Where(gd => gd.GameDayId == gameDayId))
            .ThenInclude(gd => gd.Puzzles.OrderBy(p => p.Order))
            .ThenInclude(p => p.MediaFiles)
            .Include(x => x.GameDays.Where(gd => gd.GameDayId == gameDayId))
            .ThenInclude(gd => gd.GameDayMedia)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse<GameDayDetailsViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        var gameDay = game.GameDays.FirstOrDefault();
        if (gameDay == null || !gameDay.IsEnabled)
        {
            return ServiceResponse<GameDayDetailsViewModel>.Failure("Η ημέρα δεν βρέθηκε.");
        }

        var formattedTitle = $"{game.OrderTitle} {game.Year}";
        if (!string.IsNullOrWhiteSpace(game.Title))
        {
            formattedTitle += $" - {game.Title}";
        }

        var viewModel = new GameDayDetailsViewModel
        {
            GameDayId = gameDay.GameDayId,
            Name = gameDay.GameDayType.Label,
            Description = gameDay.Description,
            GameSlug = slug,
            GameFormattedTitle = formattedTitle,
            Puzzles = gameDay.Puzzles.OrderBy(p => p.Order).Select(p => new PuzzleDetailsViewModel
            {
                PuzzleId = p.PuzzleId,
                GameDayId = p.GameDayId,
                Order = p.Order,
                Group = p.Group,
                Question = p.Question,
                Answer = p.Answer,
                QuestionMedia = p.MediaFiles
                    .Where(m => m.MediaType == "Q")
                    .Select(m => new MediaFileViewModel { MediaFileId = m.PuzzleMediaId, FileName = m.FileName, Path = m.Path }).ToList(),
                AnswerMedia = p.MediaFiles
                    .Where(m => m.MediaType == "A")
                    .Select(m => new MediaFileViewModel { MediaFileId = m.PuzzleMediaId, FileName = m.FileName, Path = m.Path }).ToList()
            }).ToList(),
            Media = gameDay.GameDayMedia.Select(m => new MediaFileViewModel { MediaFileId = m.GameDayMediaId, FileName = m.FileName, Path = m.Path }).ToList()
        };

        return ServiceResponse<GameDayDetailsViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse<GameDaysViewModel>> GetGameDaysViewModelAsync(string slug)
    {
        var game = await _context.Games
            .AsNoTracking()
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.GameDays)
            .ThenInclude(x => x.GameDayType)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse<GameDaysViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<GameDaysViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var formattedTitle = $"{game.OrderTitle} {game.Year}";
        if (!string.IsNullOrWhiteSpace(game.Title))
        {
            formattedTitle += $" - {game.Title}";
        }

        var viewModel = new GameDaysViewModel
        {
            GameId = game.GameId,
            Slug = game.Slug,
            FormattedTitle = formattedTitle,
            Days = game.GameDays.Select(x => new GameDayViewModel
            {
                GameDayId = x.GameDayId, Name = x.GameDayType.Label, IsEnabled = x.IsEnabled
            }).ToList()
        };

        return ServiceResponse<GameDaysViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse<GameDayActionsViewModel>> GetGameDayActionsAsync(string slug, long gameDayId)
    {
        var game = await _context.Games
            .AsNoTracking()
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.GameDays)
            .ThenInclude(x => x.GameDayType)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse<GameDayActionsViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<GameDayActionsViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var gameDay = game.GameDays.FirstOrDefault(x => x.GameDayId == gameDayId);
        if (gameDay == null)
        {
            return ServiceResponse<GameDayActionsViewModel>.Failure("Η ημέρα δεν βρέθηκε.");
        }

        var formattedTitle = $"{game.OrderTitle} {game.Year}";
        if (!string.IsNullOrWhiteSpace(game.Title))
        {
            formattedTitle += $" - {game.Title}";
        }

        var viewModel = new GameDayActionsViewModel
        {
            GameDayId = gameDay.GameDayId, GameSlug = game.Slug, GameDayName = gameDay.GameDayType.Label, GameFormattedTitle = formattedTitle
        };

        return ServiceResponse<GameDayActionsViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse<GameDayUpdateViewModel>> PrepareGameDayUpdateAsync(string slug, long gameDayId)
    {
        var game = await _context.Games
            .AsNoTracking()
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.GameDays)
            .ThenInclude(x => x.GameDayType)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse<GameDayUpdateViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<GameDayUpdateViewModel>.Warning(
                "Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var gameDay = game.GameDays.FirstOrDefault(x => x.GameDayId == gameDayId);
        if (gameDay == null)
        {
            return ServiceResponse<GameDayUpdateViewModel>.Failure("Η ημέρα δεν βρέθηκε.");
        }

        var formattedTitle = $"{game.OrderTitle} {game.Year}";
        if (!string.IsNullOrWhiteSpace(game.Title))
        {
            formattedTitle += $" - {game.Title}";
        }

        var viewModel = new GameDayUpdateViewModel
        {
            GameDayId = gameDay.GameDayId,
            GameSlug = game.Slug,
            GameDayName = gameDay.GameDayType.Label,
            GameFormattedTitle = formattedTitle,
            Title = gameDay.Title,
            Description = gameDay.Description
        };

        return ServiceResponse<GameDayUpdateViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> UpdateGameDayAsync(GameDayUpdateViewModel viewModel)
    {
        var gameDay = await _context.GameDays
            .Include(gd => gd.Game)
            .ThenInclude(g => g.Host)
            .ThenInclude(h => h.Managers)
            .FirstOrDefaultAsync(gd => gd.GameDayId == viewModel.GameDayId);

        if (gameDay == null)
        {
            return ServiceResponse.Failure("Η ημέρα δεν βρέθηκε.");
        }

        if (!HasGameAccess(gameDay.Game))
        {
            return ServiceResponse.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        gameDay.Title = viewModel.Title;
        gameDay.Description = viewModel.Description;

        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Η ημέρα ενημερώθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse<GameUpdateViewModel>> PrepareGameUpdateViewModelAsync(string slug,
        GameUpdateViewModel? gameUpdateViewModel = null)
    {
        var game = await _context.Games
            .AsNoTracking()
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.Winner)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse<GameUpdateViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<GameUpdateViewModel>.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        SelectList? selectList = null;
        if (User.IsInRole("Admin"))
        {
            var selectListItems = (await _context.Teams.ToListAsync())
                .Select(x => new SelectListItem { Value = x.TeamId.ToString(), Text = x.Name })
                .OrderBy(x => x.Text)
                .ToList();
            selectListItems.Insert(0, new SelectListItem { Value = "", Text = "Επιλογή Ομάδας" });
            selectList = new SelectList(selectListItems, "Value", "Text");
        }

        gameUpdateViewModel ??= new GameUpdateViewModel
        {
            GameId = game.GameId,
            Slug = game.Slug,
            UpdatedSlug = game.Slug,
            Year = game.Year,
            Order = game.Order,
            HostId = game.Host.TeamId,
            WinnerId = game.Winner.TeamId,
            Description = game.Description,
            Image = game.Image
        };

        if (selectList != null)
        {
            gameUpdateViewModel.HostSelectList = selectList;
            gameUpdateViewModel.WinnerSelectList = selectList;
        }

        return ServiceResponse<GameUpdateViewModel>.Successful(gameUpdateViewModel);
    }

    public async Task<ServiceResponse<string>> UpdateGameAsync(GameUpdateViewModel gameUpdateViewModel,
        IFormFile? image, ModelStateDictionary modelState)
    {
        var game = await _context.Games
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .FirstOrDefaultAsync(x => x.GameId == gameUpdateViewModel.GameId);

        if (game == null)
        {
            return ServiceResponse<string>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<string>.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        if (User.IsInRole("Admin"))
        {
            var allGames = await _context.Games.Where(x => x.GameId != game.GameId).ToListAsync();

            if (allGames.Any(x => x.Year == gameUpdateViewModel.Year))
            {
                modelState.AddModelError("Year", "Υπάρχει ήδη παιχνίδι για αυτή τη χρονιά.");
            }

            if (allGames.Any(x => x.Order == gameUpdateViewModel.Order))
            {
                modelState.AddModelError("Order", "Υπάρχει ήδη παιχνίδι για αυτή τη σειρά.");
            }

            var newSlug = game.Slug;
            if (!string.IsNullOrWhiteSpace(gameUpdateViewModel.UpdatedSlug) &&
                gameUpdateViewModel.UpdatedSlug != game.Slug)
            {
                newSlug = SlugGenerator.GenerateSlug(gameUpdateViewModel.UpdatedSlug);
                if (await _context.Games.AnyAsync(x => x.Slug == newSlug && x.GameId != game.GameId))
                {
                    modelState.AddModelError("UpdatedSlug", "Το slug χρησιμοποιείται ήδη.");
                }
            }

            if (!modelState.IsValid)
            {
                return ServiceResponse<string>.Failure();
            }

            if (game.Year != gameUpdateViewModel.Year)
            {
                var moveResult = _directoryService.MoveDirectory($"games/{game.Year}",
                    $"games/{gameUpdateViewModel.Year}");
                if (!moveResult.Success)
                {
                    modelState.AddModelError("Year", "Αποτυχία μετονομασίας του φακέλου πολυμέσων.");
                    return ServiceResponse<string>.Failure(
                        "Αποτυχία μετονομασίας του φακέλου πολυμέσων του παιχνιδιού.");
                }

                await UpdateGameMediaPathsAsync(game, game.Year, gameUpdateViewModel.Year);
            }

            game.Year = gameUpdateViewModel.Year;
            game.Order = gameUpdateViewModel.Order;
            game.HostId = gameUpdateViewModel.HostId;
            game.WinnerId = gameUpdateViewModel.WinnerId;
            game.Slug = newSlug;
        }

        game.Description = gameUpdateViewModel.Description;

        if (image is not null)
        {
            var storageDirectory = $"games/{game.Year}";
            var saveResult = await _directoryService.SaveFileAsync(image, storageDirectory, "profile");
            
            if (!saveResult.Success)
            {
                _logger.LogError("Failed to save profile image for game {GameSlug}: {Message}", game.Slug,
                    saveResult.Message);
                modelState.AddModelError("Image", "Σφάλμα κατά την αποθήκευση της εικόνας.");
                return ServiceResponse<string>.Failure("Σφάλμα κατά την αποθήκευση της εικόνας.");
            }

            game.Image = saveResult.Data!;
        }

        await _context.SaveChangesAsync();

        return ServiceResponse<string>.Successful(game.Slug, "Οι αλλαγές αποθηκεύτηκαν.");
    }

    public async Task<ServiceResponse> UpdateGameDaysAsync(string slug, List<long> enabledDayIds)
    {
        var game = await _context.Games
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.GameDays)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        foreach (var gameDay in game.GameDays)
        {
            gameDay.IsEnabled = enabledDayIds.Contains(gameDay.GameDayId);
        }

        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Οι αλλαγές αποθηκεύτηκαν.");
    }

    public async Task<ServiceResponse<GameMediaViewModel>> GetGameMediaViewModelAsync(string slug)
    {
        var game = await _context.Games
            .AsNoTracking()
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.MediaFiles)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse<GameMediaViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse<GameMediaViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var viewModel = new GameMediaViewModel
        {
            GameId = game.GameId,
            Slug = game.Slug,
            FormattedTitle = FormatGameTitle(game),
            Media = game.MediaFiles
                .OrderByDescending(x => x.TimeStamp)
                .Select(x => new GameMediaItemViewModel
                {
                    GameMediaId = x.GameMediaId,
                    Path = x.Path,
                    OriginalFileName = x.OriginalFileName,
                    FileName = x.FileName,
                    TimeStamp = x.TimeStamp
                }).ToList()
        };

        return ServiceResponse<GameMediaViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> UploadGameMediaAsync(string slug, List<IFormFile> files)
    {
        var game = await _context.Games
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var storageDirectory = $"games/{game.Year}";

        foreach (var file in files)
        {
            var saveResult = await _directoryService.SaveFileAsync(file, storageDirectory);
            if (!saveResult.Success)
            {
                _logger.LogError("Failed to upload file {FileName} for game {GameSlug}: {Message}", file.FileName, slug,
                    saveResult.Message);
                return ServiceResponse.Failure($"Σφάλμα κατά την αποθήκευση του αρχείου {file.FileName}.");
            }

            var newMedia = new GameMedia
            {
                GameId = game.GameId, Path = saveResult.Data!, OriginalFileName = file.FileName, FileName = Path.GetFileName(saveResult.Data!)
            };
            _context.GameMedia.Add(newMedia);
        }

        await _context.SaveChangesAsync();
        return ServiceResponse.Successful("Τα αρχεία πολυμέσων ανέβηκαν με επιτυχία.");
    }

    public async Task<ServiceResponse> DeleteGameMediaAsync(string slug, long gameMediaId)
    {
        var game = await _context.Games
            .Include(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.MediaFiles)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (!HasGameAccess(game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var media = game.MediaFiles.FirstOrDefault(x => x.GameMediaId == gameMediaId);
        if (media == null)
        {
            return ServiceResponse.Failure("Το αρχείο πολυμέσων δεν βρέθηκε.");
        }

        var deleteResult = _directoryService.DeleteFile(media.Path);
        if (!deleteResult.Success)
        {
            _logger.LogError("Failed to delete physical file {FilePath} for media ID {MediaId}: {Message}", media.Path,
                media.GameMediaId, deleteResult.Message);
            return ServiceResponse.Failure("Σφάλμα κατά τη διαγραφή του αρχείου από το δίσκο.");
        }

        _context.GameMedia.Remove(media);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Το αρχείο πολυμέσων διαγράφηκε με επιτυχία.");
    }

    public async Task<ServiceResponse<GameDayMediaViewModel>> GetGameDayMediaViewModelAsync(string slug, long gameDayId)
    {
        var gameDay = await _context.GameDays
            .AsNoTracking()
            .Include(x => x.GameDayType)
            .Include(x => x.Game)
            .ThenInclude(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.GameDayMedia)
            .FirstOrDefaultAsync(x => x.GameDayId == gameDayId);

        if (gameDay == null)
        {
            return ServiceResponse<GameDayMediaViewModel>.Failure("Η ημέρα δεν βρέθηκε.");
        }

        if (!HasGameAccess(gameDay.Game))
        {
            return ServiceResponse<GameDayMediaViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var viewModel = new GameDayMediaViewModel
        {
            GameDayId = gameDay.GameDayId,
            GameSlug = gameDay.Game.Slug,
            GameDayName = gameDay.GameDayType.Label,
            GameFormattedTitle = FormatGameTitle(gameDay.Game),
            Media = gameDay.GameDayMedia
                .OrderByDescending(x => x.TimeStamp)
                .Select(x => new GameDayMediaItemViewModel
                {
                    GameDayMediaId = x.GameDayMediaId,
                    Path = x.Path,
                    OriginalFileName = x.OriginalFileName,
                    FileName = x.FileName,
                    TimeStamp = x.TimeStamp
                }).ToList()
        };

        return ServiceResponse<GameDayMediaViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> UploadGameDayMediaAsync(string slug, long gameDayId, List<IFormFile> files)
    {
        var gameDay = await _context.GameDays
            .Include(x => x.Game)
            .ThenInclude(x => x.Host)
            .ThenInclude(x => x.Managers)
            .FirstOrDefaultAsync(x => x.GameDayId == gameDayId);

        if (gameDay == null)
        {
            return ServiceResponse.Failure("Η ημέρα δεν βρέθηκε.");
        }

        if (!HasGameAccess(gameDay.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var storageDirectory = $"games/{gameDay.Game.Year}";

        foreach (var file in files)
        {
            var saveResult = await _directoryService.SaveFileAsync(file, storageDirectory);
            if (!saveResult.Success)
            {
                _logger.LogError("Failed to upload file {FileName} for game day {GameDayId}: {Message}", file.FileName,
                    gameDayId, saveResult.Message);
                return ServiceResponse.Failure($"Σφάλμα κατά την αποθήκευση του αρχείου {file.FileName}.");
            }

            var newMedia = new GameDayMedia
            {
                GameDayId = gameDay.GameDayId, Path = saveResult.Data!, OriginalFileName = file.FileName, FileName = Path.GetFileName(saveResult.Data!)
            };
            _context.GameDayMedia.Add(newMedia);
        }

        await _context.SaveChangesAsync();
        return ServiceResponse.Successful("Τα αρχεία πολυμέσων ανέβηκαν με επιτυχία.");
    }

    public async Task<ServiceResponse> DeleteGameDayMediaAsync(string slug, long gameDayId, long gameDayMediaId)
    {
        var gameDay = await _context.GameDays
            .Include(x => x.Game)
            .ThenInclude(x => x.Host)
            .ThenInclude(x => x.Managers)
            .Include(x => x.GameDayMedia)
            .FirstOrDefaultAsync(x => x.GameDayId == gameDayId);

        if (gameDay == null)
        {
            return ServiceResponse.Failure("Η ημέρα δεν βρέθηκε.");
        }

        if (!HasGameAccess(gameDay.Game))
        {
            return ServiceResponse.Failure("Δεν έχετε δικαίωμα πρόσβασης σε αυτό το παιχνίδι.");
        }

        var media = gameDay.GameDayMedia.FirstOrDefault(x => x.GameDayMediaId == gameDayMediaId);
        if (media == null)
        {
            return ServiceResponse.Failure("Το αρχείο πολυμέσων δεν βρέθηκε.");
        }

        var deleteResult = _directoryService.DeleteFile(media.Path);
        if (!deleteResult.Success)
        {
            _logger.LogError("Failed to delete physical file {FilePath} for media ID {MediaId}: {Message}", media.Path,
                media.GameDayMediaId, deleteResult.Message);
            return ServiceResponse.Failure("Σφάλμα κατά τη διαγραφή του αρχείου από το δίσκο.");
        }

        _context.GameDayMedia.Remove(media);
        await _context.SaveChangesAsync();

        return ServiceResponse.Successful("Το αρχείο πολυμέσων διαγράφηκε με επιτυχία.");
    }

    private async Task UpdateGameMediaPathsAsync(Game game, short oldYear, short newYear)
    {
        var oldPrefix = $"media/games/{oldYear}/";
        var newPrefix = $"media/games/{newYear}/";

        if (game.Image is not null && game.Image.StartsWith(oldPrefix))
        {
            game.Image = newPrefix + game.Image[oldPrefix.Length..];
        }

        var gameMedia = await _context.GameMedia
            .Where(m => m.GameId == game.GameId)
            .ToListAsync();
        var gameDayMedia = await _context.GameDayMedia
            .Where(m => m.GameDay.GameId == game.GameId)
            .ToListAsync();
        var puzzleMedia = await _context.PuzzleMedia
            .Where(m => m.Puzzle.GameDay.GameId == game.GameId)
            .ToListAsync();

        foreach (var media in gameMedia.Where(m => m.Path.StartsWith(oldPrefix)))
        {
            media.Path = newPrefix + media.Path[oldPrefix.Length..];
        }

        foreach (var media in gameDayMedia.Where(m => m.Path.StartsWith(oldPrefix)))
        {
            media.Path = newPrefix + media.Path[oldPrefix.Length..];
        }

        foreach (var media in puzzleMedia.Where(m => m.Path.StartsWith(oldPrefix)))
        {
            media.Path = newPrefix + media.Path[oldPrefix.Length..];
        }
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

    private bool HasGameAccess(Game game)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return game.Host.Managers.Any(m => m.AppUserId == userId);
    }
}
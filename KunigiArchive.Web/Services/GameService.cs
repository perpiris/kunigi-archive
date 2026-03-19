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
            .OrderBy(x => x.GameId)
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
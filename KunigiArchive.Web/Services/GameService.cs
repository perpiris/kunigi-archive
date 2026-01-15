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
        long? userId = null, bool isAdmin = false)
    {
        var query = _context.Games.AsQueryable();
        if (includeFullDetails)
        {
            query = query
                .Include(x => x.Host)
                .Include(x => x.Winner)
                .Include(x => x.GameDays)
                .ThenInclude(x => x.GameDayType)
                .AsSplitQuery();
        }

        var game = await query.FirstOrDefaultAsync(x => x.Slug == slug);
        if (game == null)
        {
            return ServiceResponse<GameDetailsViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (userId.HasValue && !await CanUserAccessGameAsync(userId.Value, slug, isAdmin))
        {
            return ServiceResponse<GameDetailsViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτή την ομάδα.");
        }

        return ServiceResponse<GameDetailsViewModel>.Successful(game.MapToGameDetailsViewModel(includeFullDetails));
    }

    public async Task<ServiceResponse<GameDaysViewModel>> GetGameDaysViewModelAsync(string slug, long? userId = null,
        bool isAdmin = false)
    {
        var game = await _context.Games
            .AsNoTracking()
            .Include(x => x.GameDays)
            .ThenInclude(x => x.GameDayType)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (game == null)
        {
            return ServiceResponse<GameDaysViewModel>.Failure("Το παιχνίδι δεν βρέθηκε.");
        }

        if (userId.HasValue && !await CanUserAccessGameAsync(userId.Value, slug, isAdmin))
        {
            return ServiceResponse<GameDaysViewModel>.Warning("Δεν έχετε δικαίωμα πρόσβασης σε αυτή την ομάδα.");
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

    private async Task<bool> CanUserAccessGameAsync(long userId, string slug, bool isAdmin = false)
    {
        if (isAdmin)
        {
            return true;
        }

        var team = await _context.Teams.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);
        if (team is null)
        {
            return false;
        }

        return await _context.TeamManagers.AnyAsync(x => x.TeamId == team.TeamId && x.AppUserId == userId);
    }

    public async Task<ServiceResponse<bool>> ToggleGameDayAsync(long gameDayId)
    {
        var gameDay = await _context.GameDays.FindAsync(gameDayId);
        if (gameDay == null)
        {
            return ServiceResponse<bool>.Failure("Η ημέρα δεν βρέθηκε.");
        }

        gameDay.IsEnabled = !gameDay.IsEnabled;
        await _context.SaveChangesAsync();

        return ServiceResponse<bool>.Successful(gameDay.IsEnabled);
    }
}
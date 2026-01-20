using System.Security.Claims;
using KunigiArchive.Web.Services;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

[Route("games")]
public class GameController : Controller
{
    private readonly IGameService _gameService;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<IActionResult> GameList(short pageNumber = 1, short pageSize = 8, string? searchQuery = null)
    {
        var serviceResponse = await _gameService.GetPaginatedGamesAsync(pageNumber, pageSize, searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(serviceResponse.Data);
    }

    [HttpGet("manage")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GameManagement(short pageNumber = 1, short pageSize = 8,
        string? searchQuery = null)
    {
        var serviceResponse = await _gameService.GetPaginatedGamesAsync(pageNumber, pageSize, searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(serviceResponse.Data);
    }

    [HttpGet("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GameCreate()
    {
        var serviceResponse = await _gameService.PrepareGameCreateViewModelViewModel();
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameManagement));
        }

        return View(serviceResponse.Data);
    }

    [HttpPost("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GameCreate(GameCreateViewModel gameCreateViewModel)
    {
        if (!ModelState.IsValid)
        {
            var prepareResponse = await _gameService.PrepareGameCreateViewModelViewModel(gameCreateViewModel);
            if (!prepareResponse.Success)
            {
                AlertMessageHandler.SetMessage(TempData, prepareResponse);
                return RedirectToAction(nameof(GameManagement));
            }

            return View(prepareResponse.Data);
        }

        var serviceResponse = await _gameService.CreateGameAsync(gameCreateViewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameManagement));
        }

        var prepareAgainResponse = await _gameService.PrepareGameCreateViewModelViewModel(gameCreateViewModel);
        if (!prepareAgainResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameManagement));
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(prepareAgainResponse.Data);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("{slug}/actions")]
    public async Task<IActionResult> GameActions(string slug)
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var isAdmin = User.IsInRole("Admin");

        var serviceResponse = await _gameService.GetGameBySlugAsync(slug, false, userId, isAdmin);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("{slug}/days")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameDays(string slug)
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var isAdmin = User.IsInRole("Admin");

        var serviceResponse = await _gameService.GetGameDaysViewModelAsync(slug, userId, isAdmin);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameActions), new { slug });
    }

    [HttpPost("{slug}/days/{gameDayId:long}/toggle")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ToggleGameDay(string slug, long gameDayId)
    {
        var serviceResponse = await _gameService.ToggleGameDayAsync(gameDayId);

        if (serviceResponse.Success)
        {
            var message = serviceResponse.Data ? "Η ημέρα ενεργοποιήθηκε." : "Η ημέρα απενεργοποιήθηκε.";
            AlertMessageHandler.SetMessage(TempData, ServiceResponse.Successful(message));
        }
        else
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
        }

        return RedirectToAction(nameof(GameDays), new { slug });
    }
}
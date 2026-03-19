using System.Security.Claims;
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

    [HttpGet("{slug}/update")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GameUpdate(string slug)
    {
        var serviceResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug);
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameManagement));
        }

        return View(serviceResponse.Data);
    }

    [HttpPost("{slug}/update")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GameUpdate(string slug, GameUpdateViewModel gameUpdateViewModel)
    {
        if (!ModelState.IsValid)
        {
            var prepareResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug, gameUpdateViewModel);
            if (!prepareResponse.Success)
            {
                AlertMessageHandler.SetMessage(TempData, prepareResponse);
                return RedirectToAction(nameof(GameManagement));
            }

            return View(prepareResponse.Data);
        }

        var serviceResponse = await _gameService.UpdateGameAsync(gameUpdateViewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameActions), new { slug = serviceResponse.Data });
        }

        var prepareAgainResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug, gameUpdateViewModel);
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

    [HttpPost("{slug}/days")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateGameDays(string slug, List<long> enabledDayIds)
    {
        var serviceResponse = await _gameService.UpdateGameDaysAsync(slug, enabledDayIds);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameActions), new { slug });
    }
}
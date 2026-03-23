using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

[Route("games")]
public class GameController : Controller
{
    private readonly IGameService _gameService;
    private readonly IPuzzleService _puzzleService;

    public GameController(IGameService gameService, IPuzzleService puzzleService)
    {
        _gameService = gameService;
        _puzzleService = puzzleService;
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

    [HttpGet("{slug}")]
    public async Task<IActionResult> GameDetails(string slug)
    {
        var serviceResponse = await _gameService.GetGameBySlugAsync(slug, true, true);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameList));
    }

    [HttpGet("{slug}/day/{id:long}")]
    public async Task<IActionResult> GameDayDetails(string slug, long id)
    {
        var serviceResponse = await _gameService.GetGameBySlugAsync(slug, true, true);
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameList));
        }

        var gameDay = serviceResponse.Data!.GameDays.FirstOrDefault(d => d.GameDayId == id);
        if (gameDay == null)
        {
            return RedirectToAction(nameof(GameDetails), new { slug });
        }

        ViewBag.GameSlug = slug;
        ViewBag.GameTitle = serviceResponse.Data!.FormattedTitle();
        return View(gameDay);
    }

    [HttpGet("{slug}/update")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameUpdate(string slug)
    {
        var serviceResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug);
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameActions), new { slug });
        }

        return View(serviceResponse.Data);
    }

    [HttpPost("{slug}/update")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameUpdate(string slug, GameUpdateViewModel gameUpdateViewModel, IFormFile? image)
    {
        if (!ModelState.IsValid)
        {
            var prepareResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug, gameUpdateViewModel);
            if (!prepareResponse.Success)
            {
                AlertMessageHandler.SetMessage(TempData, prepareResponse);
                return RedirectToAction(nameof(GameActions), new { slug });
            }

            return View(prepareResponse.Data);
        }

        var serviceResponse = await _gameService.UpdateGameAsync(gameUpdateViewModel, image, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameActions), new { slug = serviceResponse.Data });
        }

        var prepareAgainResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug, gameUpdateViewModel);
        if (!prepareAgainResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameActions), new { slug });
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(prepareAgainResponse.Data);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("{slug}/actions")]
    public async Task<IActionResult> GameActions(string slug)
    {
        var serviceResponse = await _gameService.GetGameBySlugAsync(slug, false);
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
        var serviceResponse = await _gameService.GetGameDaysViewModelAsync(slug);
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

    [HttpGet("{slug}/day/{gameDayId:long}/puzzles")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagePuzzles(string slug, long gameDayId)
    {
        var serviceResponse = await _puzzleService.GetPuzzleManagementAsync(slug, gameDayId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameActions), new { slug });
    }

    [HttpGet("{slug}/day/{gameDayId:long}/puzzles/create")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreatePuzzle(string slug, long gameDayId)
    {
        var serviceResponse = await _puzzleService.PrepareCreateViewModelAsync(slug, gameDayId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePuzzles), new { slug, gameDayId });
    }

    [HttpPost("{slug}/day/{gameDayId:long}/puzzles/create")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreatePuzzle(string slug, long gameDayId,
        PuzzleCreateViewModel viewModel)
    {
        var serviceResponse = await _puzzleService.CreatePuzzleAsync(viewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(ManagePuzzles), new { slug, gameDayId });
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(viewModel);
    }

    [HttpGet("{slug}/day/{gameDayId:long}/puzzles/{puzzleId:long}/edit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> EditPuzzle(string slug, long gameDayId, long puzzleId)
    {
        var serviceResponse = await _puzzleService.GetPuzzleEditAsync(puzzleId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePuzzles), new { slug, gameDayId });
    }

    [HttpPost("{slug}/day/{gameDayId:long}/puzzles/{puzzleId:long}/edit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> EditPuzzle(string slug, long gameDayId, long puzzleId,
        PuzzleEditViewModel viewModel)
    {
        var serviceResponse = await _puzzleService.UpdatePuzzleAsync(viewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(ManagePuzzles), new { slug, gameDayId });
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(viewModel);
    }

    [HttpPost("{slug}/day/{gameDayId:long}/puzzles/{puzzleId:long}/delete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeletePuzzle(string slug, long gameDayId, long puzzleId)
    {
        var serviceResponse = await _puzzleService.DeletePuzzleAsync(puzzleId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePuzzles), new { slug, gameDayId });
    }
}

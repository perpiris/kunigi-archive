using KunigiArchive.Web.Services;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

[Route("games")]
public class GameController : Controller
{
    private readonly IGameService _gameService;
    private readonly IPuzzleService _puzzleService;
    private readonly IPostService _postService;

    public GameController(IGameService gameService, IPuzzleService puzzleService, IPostService postService)
    {
        _gameService = gameService;
        _puzzleService = puzzleService;
        _postService = postService;
    }

    [HttpGet]
    public async Task<IActionResult> GameList(short pageNumber = 1, short pageSize = 8, string? searchQuery = null)
    {
        ServiceResponse<PaginatedViewModel<GameDetailsViewModel>> serviceResponse = await _gameService.GetPaginatedGamesAsync(pageNumber, pageSize, searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(serviceResponse.Data);
    }

    [HttpGet("manage")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GameManagement(short pageNumber = 1, short pageSize = 8,
        string? searchQuery = null)
    {
        ServiceResponse<PaginatedViewModel<GameDetailsViewModel>> serviceResponse = await _gameService.GetPaginatedGamesAsync(pageNumber, pageSize, searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(serviceResponse.Data);
    }

    [HttpGet("create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GameCreate()
    {
        ServiceResponse<GameCreateViewModel> serviceResponse = await _gameService.PrepareGameCreateViewModelViewModel();
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
            ServiceResponse<GameCreateViewModel> prepareResponse = await _gameService.PrepareGameCreateViewModelViewModel(gameCreateViewModel);
            if (!prepareResponse.Success)
            {
                AlertMessageHandler.SetMessage(TempData, prepareResponse);
                return RedirectToAction(nameof(GameManagement));
            }

            return View(prepareResponse.Data);
        }

        ServiceResponse serviceResponse = await _gameService.CreateGameAsync(gameCreateViewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameManagement));
        }

        ServiceResponse<GameCreateViewModel> prepareAgainResponse = await _gameService.PrepareGameCreateViewModelViewModel(gameCreateViewModel);
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
        ServiceResponse<GameDetailsViewModel> serviceResponse = await _gameService.GetGameBySlugAsync(slug, true, true);
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
        ServiceResponse<GameDayDetailsViewModel> serviceResponse = await _gameService.GetGameDayDetailsForPublicViewAsync(slug, id);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameDetails), new { slug });
    }

    [HttpGet("{slug}/update")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameUpdate(string slug)
    {
        ServiceResponse<GameUpdateViewModel> serviceResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug);
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
            ServiceResponse<GameUpdateViewModel> prepareResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug, gameUpdateViewModel);
            if (!prepareResponse.Success)
            {
                AlertMessageHandler.SetMessage(TempData, prepareResponse);
                return RedirectToAction(nameof(GameActions), new { slug });
            }

            return View(prepareResponse.Data);
        }

        ServiceResponse<string> serviceResponse = await _gameService.UpdateGameAsync(gameUpdateViewModel, image, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(GameActions), new { slug = serviceResponse.Data });
        }

        ServiceResponse<GameUpdateViewModel> prepareAgainResponse = await _gameService.PrepareGameUpdateViewModelAsync(slug, gameUpdateViewModel);
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
        ServiceResponse<GameDetailsViewModel> serviceResponse = await _gameService.GetGameBySlugAsync(slug, true);
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
        ServiceResponse<GameDaysViewModel> serviceResponse = await _gameService.GetGameDaysViewModelAsync(slug);
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
        ServiceResponse serviceResponse = await _gameService.UpdateGameDaysAsync(slug, enabledDayIds);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameActions), new { slug });
    }

    [HttpGet("{slug}/media")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameMedia(string slug)
    {
        ServiceResponse<GameMediaViewModel> serviceResponse = await _gameService.GetGameMediaViewModelAsync(slug);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameActions), new { slug });
    }

    [HttpPost("{slug}/media/upload")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UploadGameMedia(string slug, List<IFormFile> files)
    {
        ServiceResponse serviceResponse = await _gameService.UploadGameMediaAsync(slug, files);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameMedia), new { slug });
    }

    [HttpPost("{slug}/media/{gameMediaId:long}/delete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteGameMedia(string slug, long gameMediaId)
    {
        ServiceResponse serviceResponse = await _gameService.DeleteGameMediaAsync(slug, gameMediaId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameMedia), new { slug });
    }

    [HttpGet("{slug}/day/{gameDayId:long}/update")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameDayUpdate(string slug, long gameDayId)
    {
        ServiceResponse<GameDayUpdateViewModel> serviceResponse = await _gameService.PrepareGameDayUpdateAsync(slug, gameDayId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameDayActions), new { slug, gameDayId });
    }

    [HttpPost("{slug}/day/{gameDayId:long}/update")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameDayUpdate(string slug, long gameDayId,
        GameDayUpdateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        ServiceResponse serviceResponse = await _gameService.UpdateGameDayAsync(viewModel);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);

        if (serviceResponse.Success)
        {
            return RedirectToAction(nameof(GameDayActions), new { slug, gameDayId });
        }

        return View(viewModel);
    }

    [HttpGet("{slug}/day/{gameDayId:long}/actions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameDayActions(string slug, long gameDayId)
    {
        ServiceResponse<GameDayActionsViewModel> serviceResponse = await _gameService.GetGameDayActionsAsync(slug, gameDayId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameDays), new { slug });
    }

    [HttpGet("{slug}/day/{gameDayId:long}/media")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GameDayMedia(string slug, long gameDayId)
    {
        ServiceResponse<GameDayMediaViewModel> serviceResponse = await _gameService.GetGameDayMediaViewModelAsync(slug, gameDayId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameDayActions), new { slug, gameDayId });
    }

    [HttpPost("{slug}/day/{gameDayId:long}/media/upload")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UploadGameDayMedia(string slug, long gameDayId, List<IFormFile> files)
    {
        ServiceResponse serviceResponse = await _gameService.UploadGameDayMediaAsync(slug, gameDayId, files);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameDayMedia), new { slug, gameDayId });
    }

    [HttpPost("{slug}/day/{gameDayId:long}/media/{gameDayMediaId:long}/delete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteGameDayMedia(string slug, long gameDayId, long gameDayMediaId)
    {
        ServiceResponse serviceResponse = await _gameService.DeleteGameDayMediaAsync(slug, gameDayId, gameDayMediaId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameDayMedia), new { slug, gameDayId });
    }

    [HttpGet("{slug}/day/{gameDayId:long}/puzzles")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagePuzzles(string slug, long gameDayId)
    {
        ServiceResponse<PuzzleManagementViewModel> serviceResponse = await _puzzleService.GetPuzzleManagementAsync(slug, gameDayId);
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
        ServiceResponse<PuzzleCreateViewModel> serviceResponse = await _puzzleService.PrepareCreateViewModelAsync(slug, gameDayId);
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
        ServiceResponse serviceResponse = await _puzzleService.CreatePuzzleAsync(viewModel, ModelState);
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
        ServiceResponse<PuzzleEditViewModel> serviceResponse = await _puzzleService.GetPuzzleEditAsync(puzzleId);
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
        ServiceResponse serviceResponse = await _puzzleService.UpdatePuzzleAsync(viewModel, ModelState);
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
        ServiceResponse serviceResponse = await _puzzleService.DeletePuzzleAsync(puzzleId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePuzzles), new { slug, gameDayId });
    }

    [HttpPost("{slug}/day/{gameDayId:long}/puzzles/{puzzleId:long}/media/{puzzleMediaId:long}/delete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeletePuzzleMedia(string slug, long gameDayId, long puzzleId, long puzzleMediaId)
    {
        ServiceResponse serviceResponse = await _puzzleService.DeletePuzzleMediaAsync(puzzleId, puzzleMediaId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(EditPuzzle), new { slug, gameDayId, puzzleId });
    }

    [HttpGet("{slug}/posts")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagePosts(string slug)
    {
        ServiceResponse<PostManagementViewModel> serviceResponse = await _postService.GetPostManagementAsync(slug);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(GameActions), new { slug });
    }

    [HttpGet("{slug}/posts/create")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreatePost(string slug)
    {
        ServiceResponse<PostCreateViewModel> serviceResponse = await _postService.PrepareCreateViewModelAsync(slug);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePosts), new { slug });
    }

    [HttpPost("{slug}/posts/create")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreatePost(string slug, PostCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        ServiceResponse serviceResponse = await _postService.CreatePostAsync(viewModel);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(ManagePosts), new { slug });
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(viewModel);
    }

    [HttpGet("{slug}/posts/{postId:long}/edit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> EditPost(string slug, long postId)
    {
        ServiceResponse<PostEditViewModel> serviceResponse = await _postService.GetPostEditAsync(postId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePosts), new { slug });
    }

    [HttpPost("{slug}/posts/{postId:long}/edit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> EditPost(string slug, long postId, PostEditViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        ServiceResponse serviceResponse = await _postService.UpdatePostAsync(viewModel);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(ManagePosts), new { slug });
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(viewModel);
    }

    [HttpPost("{slug}/posts/{postId:long}/delete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeletePost(string slug, long postId)
    {
        ServiceResponse serviceResponse = await _postService.DeletePostAsync(postId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePosts), new { slug });
    }

    [HttpGet("{slug}/posts/{postId:long}/media")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> PostMedia(string slug, long postId)
    {
        ServiceResponse<PostMediaViewModel> serviceResponse = await _postService.GetPostMediaViewModelAsync(slug, postId);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(ManagePosts), new { slug });
    }

    [HttpPost("{slug}/posts/{postId:long}/media/upload")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UploadPostMedia(string slug, long postId, List<IFormFile> files)
    {
        ServiceResponse serviceResponse = await _postService.UploadPostMediaAsync(slug, postId, files);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(PostMedia), new { slug, postId });
    }

    [HttpPost("{slug}/posts/{postId:long}/media/{postMediaId:long}/delete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeletePostMedia(string slug, long postId, long postMediaId)
    {
        ServiceResponse serviceResponse = await _postService.DeletePostMediaAsync(slug, postId, postMediaId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(PostMedia), new { slug, postId });
    }
}
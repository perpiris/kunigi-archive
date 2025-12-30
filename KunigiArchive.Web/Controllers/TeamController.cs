using KunigiArchive.Web.Mappings;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

[Route("teams")]
public class TeamController : Controller
{
    private readonly ITeamService _teamService;

    public TeamController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet]
    public async Task<IActionResult> TeamList(short pageNumber = 1, short pageSize = 10, string? searchQuery = null)
    {
        var serviceResponse = await _teamService.GetPaginatedTeamsAsync(pageNumber, pageSize, searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("manage")]
    public async Task<IActionResult> TeamManagement(short pageNumber = 1, short pageSize = 10,
        string? searchQuery = null)
    {
        var serviceResponse = await _teamService.GetPaginatedTeamsAsync(pageNumber, pageSize, searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("create")]
    public IActionResult TeamCreate()
    {
        return View(new TeamCreateViewModel());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<IActionResult> TeamCreate(TeamCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var serviceResponse = await _teamService.CreateTeamAsync(viewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(TeamManagement));
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(viewModel);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("{slug}/actions")]
    public async Task<IActionResult> TeamActions(string slug)
    {
        var serviceResponse = await _teamService.GetTeamBySlugAsync(slug, false);
        if (serviceResponse.Success)
        {
            return View(serviceResponse.Data);
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> TeamDetails(string slug)
    {
        var serviceResponse = await _teamService.GetTeamBySlugAsync(slug, true, true);
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(TeamList));
        }

        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("{slug}/update")]
    public async Task<IActionResult> TeamUpdate(string slug)
    {
        var serviceResponse = await _teamService.GetTeamBySlugAsync(slug, false);
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction("Index", "Home");
        }

        return View(serviceResponse.Data!.MapToTeamUpdateViewModel());
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{slug}/update")]
    public async Task<IActionResult> TeamUpdate(string slug, TeamUpdateViewModel viewModel, IFormFile? image)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var serviceResponse = await _teamService.UpdateTeamAsync(viewModel, image, ModelState);
        if (!serviceResponse.Success)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction("Index", "Home");
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(TeamActions), new { slug = viewModel.UpdatedSlug });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{slug}/delete")]
    public async Task<IActionResult> DeleteTeam(string slug)
    {
        var serviceResponse = await _teamService.DeleteTeamAsync(slug);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(TeamManagement));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{slug}/managers")]
    public async Task<IActionResult> TeamManagers(string slug)
    {
        var serviceResponse = await _teamService.GetTeamManagerViewModelAsync(slug);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{slug}/managers/add")]
    public async Task<IActionResult> AddTeamManager(string slug, long selectedUserId)
    {
        var serviceResponse = await _teamService.AddTeamManagerAsync(slug, selectedUserId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(TeamManagers), new { slug });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{slug}/managers/remove")]
    public async Task<IActionResult> RemoveTeamManager(string slug, long userId)
    {
        var serviceResponse = await _teamService.RemoveTeamManagerAsync(slug, userId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(TeamManagers), new { slug });
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("team-manager-dashboard")]
    public async Task<IActionResult> TeamManagerDashboard()
    {
        var serviceResponse = await _teamService.GetAllUserManagedTeams();
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction("Index", "Home");
        }

        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin, Manager")]
    [HttpGet("{slug}/media")]
    public async Task<IActionResult> TeamMedia(string slug)
    {
        var serviceResponse = await _teamService.GetTeamMediaViewModelAsync(slug);
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction("AdminDashboard", "Home");
        }

        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{slug}/media/upload")]
    public async Task<IActionResult> UploadTeamMedia(string slug, List<IFormFile> files)
    {
        var serviceResponse = await _teamService.UploadTeamMediaAsync(slug, files);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(TeamMedia), new { slug });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("{slug}/media/{teamMediaId:long}/delete")]
    public async Task<IActionResult> DeleteTeamMedia(string slug, long teamMediaId)
    {
        var serviceResponse = await _teamService.DeleteTeamMediaAsync(slug, teamMediaId);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(TeamMedia), new { slug });
    }
}
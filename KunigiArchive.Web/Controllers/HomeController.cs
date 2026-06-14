using KunigiArchive.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

public class HomeController : Controller
{
    private readonly IDirectoryService _directoryService;

    public HomeController(IDirectoryService directoryService)
    {
        _directoryService = directoryService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("error")]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }

    [HttpGet("not-found")]
    public new IActionResult NotFound()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-dashboard")]
    public IActionResult AdminDashboard()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-dashboard/media-status")]
    public IActionResult MediaStatus()
    {
        var response = _directoryService.GetMediaStatus();
        if (!response.Success)
        {
            TempData["Error"] = response.Message;
            return RedirectToAction(nameof(AdminDashboard));
        }

        return View(response.Data);
    }
}
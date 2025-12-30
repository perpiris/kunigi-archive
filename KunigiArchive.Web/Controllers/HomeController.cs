using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

public class HomeController : Controller
{
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
}
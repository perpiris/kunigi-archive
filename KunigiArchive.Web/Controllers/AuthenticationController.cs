using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Enums;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web.Controllers;

[Route("auth")]
public class AuthenticationController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IAccountService _accountService;

    public AuthenticationController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IAccountService accountService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _accountService = accountService;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError("Email", "Ο λογαριασμός είναι κλειδωμένος.");
                return View(model);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);
        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("Email", "Το email ή ο κωδικός δεν ταιριάζουν");
        return View(model);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        AlertMessageHandler.SetMessage(TempData, "Αποσυνδεθήκατε με επιτυχία.", ResponseSeverity.Success);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            await _accountService.InitiatePasswordResetAsync(viewModel.Email);

            TempData["Success"] = "Αν υπάρχει λογαριασμός με αυτό το email, θα λάβετε οδηγίες επαναφοράς κωδικού.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(viewModel);
        }
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string code, string email)
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return RedirectToAction("Index", "Home");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login");
        }

        var viewModel = new ResetPasswordViewModel { Token = code, Email = email };
        return View(viewModel);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            await _accountService.ResetPasswordAsync(viewModel.Email, viewModel.Token, viewModel.Password);

            TempData["Success"] = "Ο κωδικός σας άλλαξε με επιτυχία. Μπορείτε να συνδεθείτε με τον νέο κωδικό σας.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(viewModel);
        }
    }
}
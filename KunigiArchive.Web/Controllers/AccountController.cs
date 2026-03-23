using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KunigiArchive.Web.Controllers;

[Authorize]
[Route("account")]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("management")]
    public async Task<IActionResult> AccountManagement(short pageNumber = 1, short pageSize = 8,
        string? searchQuery = null)
    {
        var serviceResponse = await _accountService.GetPaginatedAccountsAsync(pageNumber, pageSize, searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("create")]
    public IActionResult AccountCreate()
    {
        var viewModel = new AccountCreateViewModel();
        PrepareCreateUserViewModel(viewModel);
        return View(viewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<IActionResult> AccountCreate(AccountCreateViewModel accountCreateViewModel)
    {
        if (!ModelState.IsValid)
        {
            PrepareCreateUserViewModel(accountCreateViewModel);
            return View(accountCreateViewModel);
        }

        var serviceResponse = await _accountService.CreateAccountAsync(accountCreateViewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(AccountManagement));
        }

        PrepareCreateUserViewModel(accountCreateViewModel);
        return View(accountCreateViewModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("details/{id:long}")]
    public async Task<IActionResult> AccountDetails(long id)
    {
        var serviceResponse = await _accountService.GetAccountByIdAsync(id);
        if (!serviceResponse.Success || serviceResponse.Data == null)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(AccountManagement));
        }

        PrepareEditViewModel(serviceResponse.Data);
        return View(serviceResponse.Data);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("details/{id:long}/change-role")]
    public async Task<IActionResult> ChangeRole(long id, AccountEditViewModel viewModel)
    {
        var serviceResponse = await _accountService.ChangeRoleAsync(id, viewModel.SelectedRole);
        AlertMessageHandler.SetMessage(TempData, serviceResponse);
        return RedirectToAction(nameof(AccountDetails), new { id });
    }

    [HttpGet("settings")]
    public async Task<IActionResult> AccountSettings()
    {
        var serviceResponse = await _accountService.GetAccountDetailsAsync();
        if (!serviceResponse.Success || serviceResponse.Data == null)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction("Login", "Authentication");
        }

        return View(new AccountSettingsViewModel { Email = serviceResponse.Data!.Email });
    }

    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail(AccountSettingsViewModel viewModel)
    {
        ModelState.Remove(nameof(viewModel.OldPassword));
        ModelState.Remove(nameof(viewModel.NewPassword));
        ModelState.Remove(nameof(viewModel.ConfirmNewPassword));

        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "email";
            return View(nameof(AccountSettings), viewModel);
        }

        var serviceResponse = await _accountService.ChangeEmailAsync(viewModel, ModelState);
        if (serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            return RedirectToAction(nameof(AccountSettings));
        }

        AlertMessageHandler.SetMessage(TempData, serviceResponse);

        ViewBag.ActiveTab = "email";
        return View(nameof(AccountSettings), viewModel);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(AccountSettingsViewModel viewModel)
    {
        ModelState.Remove(nameof(viewModel.Email));

        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "password";
            return View(nameof(AccountSettings), viewModel);
        }

        var serviceResponse = await _accountService.ChangePasswordAsync(viewModel, ModelState);
        if (!serviceResponse.Success)
        {
            AlertMessageHandler.SetMessage(TempData, serviceResponse);
            ViewBag.ActiveTab = "password";
            return View(nameof(AccountSettings), viewModel);
        }

        TempData["Success"] = serviceResponse.Message;
        return RedirectToAction(nameof(AccountSettings));
    }

    private static void PrepareCreateUserViewModel(AccountCreateViewModel viewModel)
    {
        viewModel.RolesList =
        [
            new SelectListItem { Value = "Admin", Text = "Γενικός Διαχειριστής" },
            new SelectListItem { Value = "Manager", Text = "Διαχειριστής Ομάδας" }
        ];
    }

    private static void PrepareEditViewModel(AccountEditViewModel viewModel)
    {
        viewModel.RolesList =
        [
            new SelectListItem { Value = "", Text = "Χωρίς Ρόλο" },
            new SelectListItem { Value = "Admin", Text = "Γενικός Διαχειριστής" },
            new SelectListItem { Value = "Manager", Text = "Διαχειριστής Ομάδας" }
        ];
    }
}
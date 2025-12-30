using KunigiArchive.Web.ViewModels.Account;
using KunigiArchive.Web.ViewModels.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KunigiArchive.Web.Services.Interfaces;

public interface IAccountService
{
    Task<ServiceResponse<PaginatedViewModel<AccountDetailsViewModel>>> GetPaginatedAccountsAsync(short pageNumber = 1,
        short pageSize = 8, string? searchQuery = null);

    Task<ServiceResponse> CreateAccountAsync(AccountCreateViewModel viewModel, ModelStateDictionary modelState);
    Task<ServiceResponse<AccountDetailsViewModel>> GetAccountDetailsAsync();
    Task<ServiceResponse> ChangeEmailAsync(AccountSettingsViewModel viewModel, ModelStateDictionary modelState);
    Task<ServiceResponse> ChangePasswordAsync(AccountSettingsViewModel viewModel, ModelStateDictionary modelState);
    Task<ServiceResponse<IEnumerable<AccountDetailsViewModel>>> GetAvailableManagersAsync();
    Task<ServiceResponse> InitiatePasswordResetAsync(string email);
    Task<ServiceResponse> ResetPasswordAsync(string email, string token, string newPassword);
}
using System.Security.Claims;
using System.Security.Cryptography;
using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.ViewModels.Account;
using KunigiArchive.Web.ViewModels.Common;
using KunigiArchive.Web.ViewModels.Team;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KunigiArchive.Web.Services;

public class AccountService : IAccountService
{
    private readonly ILogger<AccountService> _logger;
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;

    private readonly IEmailService _emailService;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User!;

    public AccountService(
        ILogger<AccountService> logger,
        DataContext context,
        UserManager<AppUser> userManager,
        IEmailService emailService,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _memoryCache = memoryCache;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResponse<PaginatedViewModel<AccountDetailsViewModel>>> GetPaginatedAccountsAsync(
        short pageNumber = 1, short pageSize = 8, string? searchQuery = null)
    {
        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(user => user.Email!.Contains(searchQuery));
        }

        var totalCount = await query.CountAsync();

        var accounts = await query
            .OrderBy(account => account.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = accounts.Select(user => user.Id).ToList();
        var userRoles = await _context.UserRoles
            .Where(userRole => userIds.Contains(userRole.UserId))
            .Join(_context.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, role.Name })
            .ToListAsync();

        var userRoleDict = userRoles
            .GroupBy(user => user.UserId)
            .ToDictionary(group => group.Key, g => g.First().Name);

        var accountList = accounts.Select(account => new AccountDetailsViewModel
        {
            AppUserId = account.Id,
            Email = account.Email!,
            Role = userRoleDict.TryGetValue(account.Id, out var role) ? role : null
        }).ToList();

        var paginatedData = new PaginatedViewModel<AccountDetailsViewModel>
        {
            Items = accountList,
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            PageSize = pageSize
        };

        return ServiceResponse<PaginatedViewModel<AccountDetailsViewModel>>.Successful(paginatedData);
    }

    public async Task<ServiceResponse> CreateAccountAsync(AccountCreateViewModel viewModel,
        ModelStateDictionary modelState)
    {
        var existingUser = await _userManager.FindByEmailAsync(viewModel.Email);
        if (existingUser is not null)
        {
            modelState.AddModelError(nameof(viewModel.Email), "Υπάρχει ήδη ένας χρήστης με αυτό το email.");
            return ServiceResponse.Failure("Υπάρχει ήδη ένας χρήστης με αυτό το email.");
        }

        var user = new AppUser { UserName = viewModel.Email, Email = viewModel.Email };

        var password = GeneratePasswordFromEmail(user.Email);
        var identityResult = await _userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            _logger.LogWarning("User creation failed for email {Email}. Errors: {Errors}", viewModel.Email,
                string.Join(", ", identityResult.Errors.Select(e => e.Description)));
            foreach (var error in identityResult.Errors)
            {
                modelState.AddModelError(string.Empty, error.Description);
            }

            return ServiceResponse.Failure("Αποτυχία δημιουργίας χρήστη.");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, viewModel.SelectedRole);
        if (!roleResult.Succeeded)
        {
            _logger.LogWarning("Failed to add role {Role} to user {Email}. Errors: {Errors}", viewModel.SelectedRole,
                user.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            foreach (var error in roleResult.Errors)
            {
                modelState.AddModelError(string.Empty, error.Description);
            }

            await _userManager.DeleteAsync(user);
            return ServiceResponse.Failure("Αποτυχία ανάθεσης ρόλου στον χρήστη.");
        }

        var emailServiceResponse = await _emailService.SendAccountCreationEmailAsync(user.Email, password);
        if (!emailServiceResponse.Success)
        {
            _logger.LogWarning("Failed to send account creation email to {Email}", user.Email);
            return ServiceResponse.Failure(
                "Ο λογαριασμός δημιουργήθηκε, αλλά δεν ήταν δυνατή η αποστολή του email με τα στοιχεία σύνδεσης.");
        }

        return ServiceResponse.Successful("Ο χρήστης δημιουργήθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse<AccountDetailsViewModel>> GetAccountDetailsAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return ServiceResponse<AccountDetailsViewModel>.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var viewModel = new AccountDetailsViewModel
        {
            AppUserId = user.Id, Email = user.Email!, Role = roles.FirstOrDefault()
        };

        return ServiceResponse<AccountDetailsViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> ChangeEmailAsync(AccountSettingsViewModel viewModel,
        ModelStateDictionary modelState)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser is null)
        {
            return ServiceResponse.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        if (string.Equals(viewModel.Email, appUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            modelState.AddModelError("Email", "Το καινούργιο email πρέπει να είναι διαφορετικό απο το παλιό.");
            return ServiceResponse.Warning("Το καινούργιο email πρέπει να είναι διαφορετικό απο το παλιό.");
        }

        var existingUser = await _userManager.FindByEmailAsync(viewModel.Email);
        if (existingUser is not null && existingUser.Id != appUser.Id)
        {
            modelState.AddModelError("Email", "Αυτό το email χρησιμοποιείται ήδη από άλλο χρήστη.");
            return ServiceResponse.Warning("Αυτό το email χρησιμοποιείται ήδη από άλλο χρήστη.");
        }

        appUser.Email = viewModel.Email;
        appUser.UserName = viewModel.Email;

        var result = await _userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            return ServiceResponse.Failure($"Αποτυχία ενημέρωσης email: {errors}");
        }

        await _userManager.SetUserNameAsync(appUser, viewModel.Email);
        return ServiceResponse.Successful("Το email ενημερώθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse> ChangePasswordAsync(AccountSettingsViewModel viewModel,
        ModelStateDictionary modelState)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser is null)
        {
            return ServiceResponse.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        if (viewModel.OldPassword is null || viewModel.NewPassword is null)
        {
            return ServiceResponse.Failure();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(appUser, viewModel.OldPassword);
        if (!passwordValid)
        {
            modelState.AddModelError("OldPassword", "Ο τρέχων κωδικός είναι λάθος.");
            return ServiceResponse.Warning("Ο τρέχων κωδικός είναι λάθος.");
        }

        var result = await _userManager.ChangePasswordAsync(appUser, viewModel.OldPassword, viewModel.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            foreach (var error in result.Errors)
            {
                modelState.AddModelError(string.Empty, error.Description);
            }

            return ServiceResponse.Failure($"Αποτυχία αλλαγής κωδικού: {errors}");
        }

        return ServiceResponse.Successful("Ο κωδικός άλλαξε επιτυχώς.");
    }

    public async Task<ServiceResponse<AccountEditViewModel>> GetAccountByIdAsync(long userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ServiceResponse<AccountEditViewModel>.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? string.Empty;

        var managedTeams = await _context.TeamManagers
            .Where(tm => tm.AppUserId == userId)
            .Include(tm => tm.Team)
            .Select(tm => new TeamDetailsViewModel
            {
                TeamId = tm.Team.TeamId, Name = tm.Team.Name, Slug = tm.Team.Slug, IsActive = tm.Team.IsActive
            })
            .ToListAsync();

        var viewModel = new AccountEditViewModel
        {
            AppUserId = user.Id, Email = user.Email!, SelectedRole = currentRole, ManagedTeams = managedTeams
        };

        return ServiceResponse<AccountEditViewModel>.Successful(viewModel);
    }

    public async Task<ServiceResponse> ChangeRoleAsync(long userId, string newRole)
    {
        var currentUserId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (userId == currentUserId)
        {
            return ServiceResponse.Failure("Δεν μπορείτε να αλλάξετε τον ρόλο του δικού σας λογαριασμού.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ServiceResponse.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault() ?? string.Empty;

        if (currentRole == newRole)
        {
            return ServiceResponse.Warning("Ο χρήστης έχει ήδη αυτόν τον ρόλο.");
        }

        if (currentRole == "Manager")
        {
            var teamManagerRecords = await _context.TeamManagers
                .Where(tm => tm.AppUserId == userId)
                .ToListAsync();

            if (teamManagerRecords.Count > 0)
            {
                _context.TeamManagers.RemoveRange(teamManagerRecords);
                await _context.SaveChangesAsync();
            }
        }

        if (!string.IsNullOrEmpty(currentRole))
        {
            var removeResult = await _userManager.RemoveFromRoleAsync(user, currentRole);
            if (!removeResult.Succeeded)
            {
                _logger.LogWarning("Failed to remove role {Role} from user {Email}", currentRole, user.Email);
                return ServiceResponse.Failure("Αποτυχία αφαίρεσης τρέχοντος ρόλου.");
            }
        }

        if (!string.IsNullOrEmpty(newRole))
        {
            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                _logger.LogWarning("Failed to add role {Role} to user {Email}", newRole, user.Email);
                if (!string.IsNullOrEmpty(currentRole))
                {
                    await _userManager.AddToRoleAsync(user, currentRole);
                }

                return ServiceResponse.Failure("Αποτυχία ανάθεσης νέου ρόλου.");
            }
        }

        return ServiceResponse.Successful("Ο ρόλος ενημερώθηκε επιτυχώς.");
    }

    public async Task<ServiceResponse<IEnumerable<AccountDetailsViewModel>>> GetAvailableManagersAsync()
    {
        var accounts = await _userManager.GetUsersInRoleAsync("Manager");

        var mappedAccounts = accounts.Select(x => new AccountDetailsViewModel { AppUserId = x.Id, Email = x.Email! })
            .ToList();

        return ServiceResponse<IEnumerable<AccountDetailsViewModel>>.Successful(mappedAccounts);
    }

    public async Task<ServiceResponse> InitiatePasswordResetAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return ServiceResponse.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        var resetCode = GenerateResetCode();

        _memoryCache.Set(
            $"PasswordReset:{email}",
            resetCode,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });

        await _emailService.SendPasswordResetEmailAsync(email, resetCode);
        return ServiceResponse.Successful("Έχει σταλεί email με οδηγίες για την επαναφορά του κωδικού.");
    }

    public async Task<ServiceResponse> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return ServiceResponse.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        if (!_memoryCache.TryGetValue($"PasswordReset:{email}", out string? storedToken) || storedToken != token)
        {
            return ServiceResponse.Failure("Ο χρήστης δεν βρέθηκε.");
        }

        var removePasswordResult = await _userManager.RemovePasswordAsync(user);
        if (!removePasswordResult.Succeeded)
        {
            var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));
            return ServiceResponse.Failure($"Σφάλμα κατά την αφαίρεση του παλιού κωδικού: {errors}");
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addPasswordResult.Succeeded)
        {
            var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
            return ServiceResponse.Failure($"Σφάλμα κατά την αφαίρεση του παλιού κωδικού: {errors}");
        }

        _memoryCache.Remove($"PasswordReset:{email}");
        return ServiceResponse.Successful("Ο κωδικός επαναφέρθηκε επιτυχώς.");
    }

    private static string GeneratePasswordFromEmail(string email)
    {
        var username = email.Split('@')[0];

        var random = new byte[2];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);

        var digit1 = random[0] % 10;
        var digit2 = random[1] % 10;
        var digit3 = (random[0] >> 4) % 10;
        var digit4 = (random[1] >> 4) % 10;

        return $"{username}{digit1}{digit2}{digit3}{digit4}";
    }

    private static string GenerateResetCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);

        var result = new char[8];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = chars[random[i] % chars.Length];
        }

        return new string(result);
    }
}
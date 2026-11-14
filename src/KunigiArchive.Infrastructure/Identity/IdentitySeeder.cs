using KunigiArchive.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KunigiArchive.Infrastructure.Identity;

internal sealed class IdentitySeeder
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AdminSeedOptions _adminSeed;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        IOptions<AdminSeedOptions> adminSeed,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminSeed = adminSeed.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (string role in Roles.All)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            EnsureSucceeded(await _roleManager.CreateAsync(new AppRole { Name = role }), $"create role '{role}'");
            _logger.LogInformation("Role {Role} created", role);
        }
    }

    // Only bootstraps the first admin. Once any admin exists the settings are ignored, so changing or
    // removing them later never creates a second admin or resets a password.
    private async Task SeedAdminAsync()
    {
        if ((await _userManager.GetUsersInRoleAsync(Roles.Admin)).Count > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_adminSeed.Email) || string.IsNullOrWhiteSpace(_adminSeed.Password))
        {
            _logger.LogWarning(
                "No admin exists and {Section}:Email / {Section}:Password are not set, so none was created",
                AdminSeedOptions.SectionName, AdminSeedOptions.SectionName);
            return;
        }

        AppUser? admin = await _userManager.FindByEmailAsync(_adminSeed.Email);
        if (admin is null)
        {
            admin = new AppUser { UserName = _adminSeed.Email, Email = _adminSeed.Email, EmailConfirmed = true };
            EnsureSucceeded(await _userManager.CreateAsync(admin, _adminSeed.Password), "create the admin user");
        }

        EnsureSucceeded(await _userManager.AddToRoleAsync(admin, Roles.Admin), "add the admin role");
        _logger.LogInformation("Admin {Email} created", _adminSeed.Email);
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to {action}: {errors}");
        }
    }
}
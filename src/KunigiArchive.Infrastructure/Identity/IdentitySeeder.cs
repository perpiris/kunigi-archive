using KunigiArchive.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KunigiArchive.Infrastructure.Identity;

internal sealed class IdentitySeeder
{
    private const string DevelopmentManagerEmail = "manager@test.com";
    private const string DevelopmentManagerPassword = "Manager@123!";

    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AdminSeedOptions _adminSeed;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        IOptions<AdminSeedOptions> adminSeed,
        IHostEnvironment environment,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminSeed = adminSeed.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminAsync();

        if (_environment.IsDevelopment())
        {
            await SeedDevelopmentManagerAsync();
        }
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

    private async Task SeedDevelopmentManagerAsync()
    {
        if (await _userManager.FindByEmailAsync(DevelopmentManagerEmail) is not null)
        {
            return;
        }

        AppUser manager = new AppUser
        {
            UserName = DevelopmentManagerEmail, Email = DevelopmentManagerEmail, EmailConfirmed = true
        };
        EnsureSucceeded(await _userManager.CreateAsync(manager, DevelopmentManagerPassword), "create the development manager");
        EnsureSucceeded(await _userManager.AddToRoleAsync(manager, Roles.Manager), "add the manager role");
        _logger.LogInformation("Development manager {Email} created", DevelopmentManagerEmail);
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
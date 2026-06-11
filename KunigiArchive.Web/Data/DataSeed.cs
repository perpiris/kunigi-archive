using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace KunigiArchive.Web.Data;

public static class DataSeed
{
    public static async Task Seed(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var roles = new[] { "Admin", "Manager" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole { Name = role });
                Log.Information($"Role '{role}' created");
            }
        }

        const string adminEmail = "iperpirakis@gmail.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(adminUser, "Admin@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Log.Information("Admin user created successfully");
            }
            else
            {
                Log.Error($"Failed to create admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (environment.IsDevelopment())
        {
            const string managerEmail = "manager@test.com";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new AppUser { UserName = managerEmail, Email = managerEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(managerUser, "Manager@123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                    Log.Information("Manager user created successfully");
                }
                else
                {
                    Log.Error(
                        $"Failed to create manager: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        if (await context.GameDayTypes.AnyAsync())
        {
            // One-time correction for databases seeded with the misspelled label.
            var misspelled = await context.GameDayTypes.FirstOrDefaultAsync(x => x.Label == "Χωρός");
            if (misspelled != null)
            {
                misspelled.Label = "Χορός";
                await context.SaveChangesAsync();
                Log.Information("Corrected GameDayType label 'Χωρός' to 'Χορός'");
            }

            return;
        }

        var gameDayTypes = new List<GameDayType>
        {
            new() { Label = "Χορός", Slug = SlugGenerator.GenerateSlug("Χορός") },
            new() { Label = "Σάββατο", Slug = SlugGenerator.GenerateSlug("Σάββατο") },
            new() { Label = "Κυριακή", Slug = SlugGenerator.GenerateSlug("Κυριακή") },
            new() { Label = "Διαδικτυακό", Slug = SlugGenerator.GenerateSlug("Διαδικτυακό") },
            new() { Label = "Παιδικό", Slug = SlugGenerator.GenerateSlug("Παιδικό") },
            new() { Label = "Εφηβικό", Slug = SlugGenerator.GenerateSlug("Εφηβικό") }
        };

        foreach (var gameDayType in gameDayTypes)
        {
            context.GameDayTypes.Add(gameDayType);
        }

        await context.SaveChangesAsync();
    }
}
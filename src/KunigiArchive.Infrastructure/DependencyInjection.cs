using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Users;
using KunigiArchive.Domain.Teams;
using KunigiArchive.Infrastructure.Identity;
using KunigiArchive.Infrastructure.Persistence;
using KunigiArchive.Infrastructure.Persistence.Queries;
using KunigiArchive.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KunigiArchive.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "KunigiArchive";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<KunigiArchiveDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<KunigiArchiveDbContext>());

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<ITeamQueries, TeamQueries>();
        services.AddScoped<IUserQueries, UserQueries>();

        services.AddIdentity(configuration);

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        KunigiArchiveDbContext dbContext = scope.ServiceProvider.GetRequiredService<KunigiArchiveDbContext>();
        await dbContext.Database.MigrateAsync();

        IdentitySeeder seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }

    private static void AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<KunigiArchiveDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));
        services.AddScoped<IdentitySeeder>();
    }
}
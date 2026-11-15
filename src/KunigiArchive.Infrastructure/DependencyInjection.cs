using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Users;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Teams;
using KunigiArchive.Infrastructure.Email;
using KunigiArchive.Infrastructure.Html;
using KunigiArchive.Infrastructure.Identity;
using KunigiArchive.Infrastructure.Media;
using KunigiArchive.Infrastructure.Persistence;
using KunigiArchive.Infrastructure.Persistence.Queries;
using KunigiArchive.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();

        services.AddIdentity(configuration);
        services.AddMediaStorage(configuration);
        services.AddEmail(configuration);

        return services;
    }

    // Not validated on start: the site runs without email settings, only sending fails (and the admin is told).
    private static void AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .PostConfigure<IHostEnvironment>((options, environment) =>
            {
                if (!string.IsNullOrWhiteSpace(options.PickupDirectory))
                {
                    options.PickupDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.PickupDirectory));
                }
            });

        services.AddScoped<IAccountEmails, SmtpAccountEmails>();
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        KunigiArchiveDbContext dbContext = scope.ServiceProvider.GetRequiredService<KunigiArchiveDbContext>();
        await dbContext.Database.MigrateAsync();

        IdentitySeeder seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }

    private static void AddMediaStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MediaStorageOptions>()
            .Bind(configuration.GetSection(MediaStorageOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.RootPath),
                $"{MediaStorageOptions.SectionName}:RootPath is not configured.")
            .ValidateOnStart();

        services.AddOptions<MediaStorageOptions>()
            .PostConfigure<IHostEnvironment>((options, environment) =>
            {
                if (!string.IsNullOrWhiteSpace(options.RootPath))
                {
                    options.RootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.RootPath));
                }
            });

        services.AddSingleton<IMediaStorage, LocalMediaStorage>();
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
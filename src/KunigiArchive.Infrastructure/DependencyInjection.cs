using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Common;
using KunigiArchive.Application.GameDays;
using KunigiArchive.Application.Games;
using KunigiArchive.Application.Media;
using KunigiArchive.Application.Posts;
using KunigiArchive.Application.Teams;
using KunigiArchive.Application.Users;
using KunigiArchive.Domain.GameDays;
using KunigiArchive.Domain.Games;
using KunigiArchive.Domain.Posts;
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
        services.AddScoped<IGameQueries, GameQueries>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPostQueries, PostQueries>();
        services.AddScoped<IGameDayRepository, GameDayRepository>();
        services.AddScoped<IGameDayQueries, GameDayQueries>();
        services.AddScoped<ISlugHistory, SlugHistory>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();

        services.AddIdentity(configuration);
        services.AddMediaStorage(configuration);
        services.AddEmail(configuration);

        return services;
    }

    // Not validated on start: the site runs without email settings, only sending fails (logged as an error).
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

        // Account emails are queued and sent in the background, with retries (#20).
        services.AddSingleton<EmailQueue>();
        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<IAccountEmails, QueuedAccountEmails>();
        services.AddHostedService<EmailSendingService>();
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
                options.Password.RequiredLength = PasswordRules.MinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                // Not about email: DisabledUserConfirmation refuses disabled accounts (#24).
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddRoles<AppRole>()
            .AddClaimsPrincipalFactory<AppUserClaimsFactory>()
            .AddUserConfirmation<DisabledUserConfirmation>()
            .AddEntityFrameworkStores<KunigiArchiveDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Password reset links expire after an hour (the email says so).
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));

        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));
        services.AddScoped<IdentitySeeder>();
    }
}

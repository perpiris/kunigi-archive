using System.IO.Compression;
using System.Threading.RateLimiting;
using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Services;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.Settings;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Web.Extensions;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
}

public static class ServiceCollectionExtensions
{
    // Keep in sync with client_max_body_size in nginx-limits.conf.
    public const long MaxRequestBodyBytes = 250L * 1024 * 1024;

    // Also enforced per file in DirectoryService.SaveFileAsync.
    public const long MaxUploadFileBytes = 100L * 1024 * 1024;

    public static IServiceCollection AddWebInfrastructure(this IServiceCollection services)
    {
        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = MaxUploadFileBytes;
            options.ValueCountLimit = 4096;
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.SmallestSize);

        services.AddWebOptimizer(pipeline =>
        {
            pipeline.MinifyJsFiles("js/**/*.js");
            pipeline.MinifyCssFiles("css/**/*.css", "lib/bootstrap/dist/css/**/*.css",
                "lib/fancybox/**/*.css", "lib/quill/**/*.css");
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                    }));
        });

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Production: ConnectionStrings__DefaultConnection in /opt/kunigi/app.env on the VPS.
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. " +
                "Set the ConnectionStrings__DefaultConnection environment variable.");
        }

        services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString));

        services.AddIdentity<AppUser, AppRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 1;
            })
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.AccessDeniedPath = "/";
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IDirectoryService, DirectoryService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPuzzleService, PuzzleService>();
        services.AddScoped<ITeamService, TeamService>();

        return services;
    }
}

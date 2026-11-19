using System.IO.Compression;
using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Media;
using KunigiArchive.Web.Authentication;
using KunigiArchive.Web.ErrorHandling;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace KunigiArchive.Web.Extensions;

public static class ServiceCollectionExtensions
{
    // Same setup as the owner's other apps (pulse-wallet): levels, overrides and properties come from appsettings;
    // the sinks and their format are chosen here. (Configuration merges WriteTo arrays by index, so sinks set per
    // environment in appsettings get mixed up.)
    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddSerilog((serviceProvider, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext();

            if (environment.IsDevelopment())
            {
                // Readable, coloured text for a person watching the terminal.
                logger.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <cid:{CorrelationId}>{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code);
            }
            else
            {
                // One JSON object per line: every property (CorrelationId, RequestPath, ids...) is a searchable
                // field, and there are no colour codes in the container logs (`docker logs`).
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            }

            // Rotating files, as the old site had: a file a day, a new one past 10 MB, the last 30 kept. Only where
            // LogFile:Path is set (the server, on a mounted folder, so the files outlive a redeploy).
            string? path = configuration["LogFile:Path"];
            if (!string.IsNullOrWhiteSpace(path))
            {
                logger.WriteTo.File(
                    new RenderedCompactJsonFormatter(),
                    path,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 30,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }
        });

        return services;
    }

    // Settings for running behind nginx-proxy in Docker; all of them are harmless locally.
    public static IServiceCollection AddHosting(this IServiceCollection services, IConfiguration configuration)
    {
        // Without a persisted key ring every restart invents new keys: everyone is signed out and open forms
        // fail their antiforgery check. The container mounts a folder for them (docker-compose.yml).
        // The name stays "KunigiArchive.Web2" (the project's name before it replaced the old site): keys made
        // under it only work with it, so changing it would sign everyone out and void the stored key ring.
        string? keysPath = configuration["DataProtection:KeysPath"];
        IDataProtectionBuilder dataProtection = services.AddDataProtection().SetApplicationName("KunigiArchive.Web2");
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        // nginx-proxy terminates TLS. Its X-Forwarded-* headers give the real scheme (https, so redirects,
        // HSTS and secure cookies work) and client IP. Any sender is trusted because the container publishes
        // no ports: the only way in is through the proxy on the Docker network.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IServiceCollection AddWebAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/auth/login";
            options.AccessDeniedPath = "/access-denied";

            // The API answers with status codes; only pages redirect to the login page.
            options.Events.OnRedirectToLogin = context => RedirectOrStatus(context, StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => RedirectOrStatus(context, StatusCodes.Status403Forbidden);
        });

        // Roles are read from the sign-in cookie. Re-checking the security stamp every minute (default: 30)
        // means a role change or a new temporary password takes effect almost at once. Cheap with a handful of users.
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(1));

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }

    private static Task RedirectOrStatus(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        if (context.Request.IsApiRequest())
        {
            context.Response.StatusCode = statusCode;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }

    // Pages and API responses (HTML, JSON). Static assets are precompressed at build (MapStaticAssets), media
    // files are already compressed (WebP, MP4). Brotli first, gzip for older clients.
    public static IServiceCollection AddCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

        return services;
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = MediaRules.MaxRequestBytes);

        services.AddControllers();
        services.AddOpenApi();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
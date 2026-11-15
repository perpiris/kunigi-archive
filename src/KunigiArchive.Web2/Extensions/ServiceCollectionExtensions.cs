using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Media;
using KunigiArchive.Web2.Authentication;
using KunigiArchive.Web2.ErrorHandling;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace KunigiArchive.Web2.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((serviceProvider, logger) => logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext());

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
using KunigiArchive.Web2.ErrorHandling;
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
            options.AccessDeniedPath = "/";
        });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
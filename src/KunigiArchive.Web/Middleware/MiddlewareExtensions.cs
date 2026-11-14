namespace KunigiArchive.Web.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestCorrelation(this IApplicationBuilder app)
        => app.UseMiddleware<RequestCorrelationMiddleware>();

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}

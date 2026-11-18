using System.Security.Cryptography;

namespace KunigiArchive.Web2.Middleware;

// Security headers for every response. HSTS comes from nginx-proxy, so it isn't set here.
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    // App.razor reads the nonce from HttpContext.Items for the one inline script (Blazor's import map).
    public const string NonceKey = "csp-nonce";

    public Task InvokeAsync(HttpContext context)
    {
        IHeaderDictionary headers = context.Response.Headers;

        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Swagger UI (development only) runs inline scripts of its own.
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            string nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            context.Items[NonceKey] = nonce;

            // Everything is served from this site (fonts and libraries are self-hosted, #26). Inline styles stay
            // allowed: Blazor's enhanced navigation and the editor set style attributes. data: is for Bootstrap's
            // SVG icons, blob: for the editor's image previews.
            headers.ContentSecurityPolicy =
                "default-src 'self'; " +
                $"script-src 'self' 'nonce-{nonce}'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob:; " +
                "media-src 'self'; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'; " +
                "frame-ancestors 'none'";
        }

        return next(context);
    }
}

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace KunigiArchive.Web.Middleware;

// Limits per client IP (the real one: forwarded headers run first). Identity's lockout protects each account;
// these slow down guessing across accounts and stop the reset form from flooding an inbox.
public static class RateLimitPolicies
{
    // Login and reset-password pages: page loads and posts together.
    public const string Auth = "auth";

    // The forgot-password page, which sends an email on every post.
    public const string PasswordResetEmail = "password-reset-email";

    public static IServiceCollection AddWebRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.AddPolicy(Auth, context => PerClient(context, permits: 30, TimeSpan.FromMinutes(1)));
            options.AddPolicy(PasswordResetEmail, context => PerClient(context, permits: 10, TimeSpan.FromMinutes(15)));
        });

        return services;
    }

    private static RateLimitPartition<string> PerClient(HttpContext context, int permits, TimeSpan window)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 });
    }

    // A short page of its own: with a body, the status code pages leave it alone (no "not found" for a 429).
    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpResponse response = context.HttpContext.Response;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        response.ContentType = "text/html; charset=utf-8";
        await response.WriteAsync(
            """
            <!DOCTYPE html>
            <html lang="el"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Πολλές προσπάθειες</title></head>
            <body style="font-family: system-ui, sans-serif; max-width: 32rem; margin: 4rem auto; padding: 0 1rem;">
            <h1>Πολλές προσπάθειες</h1>
            <p>Έγιναν πολλές προσπάθειες σε λίγο χρόνο. Περιμένετε λίγα λεπτά και δοκιμάστε ξανά.</p>
            <p><a href="/">Αρχική σελίδα</a></p>
            </body></html>
            """,
            cancellationToken);
    }
}

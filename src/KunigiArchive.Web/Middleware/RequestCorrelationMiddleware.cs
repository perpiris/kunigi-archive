using System.Text.RegularExpressions;
using Serilog.Context;

namespace KunigiArchive.Web.Middleware;

// Gives every request an id (X-Correlation-Id), returned in the response and attached to every log line of the
// request, so one request's lines can be found together. Same as the old app's.
public sealed partial class RequestCorrelationMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    private const string LogProperty = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrCreateCorrelationId(context.Request);

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(LogProperty, correlationId))
        {
            await next(context);
        }
    }

    // Only short, safe ids from the client are kept, so the header can't be used to inject into the logs.
    private static string GetOrCreateCorrelationId(HttpRequest request)
    {
        string? incoming = request.Headers[HeaderName].FirstOrDefault();
        return incoming is not null && SafeId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex SafeId();
}

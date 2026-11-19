using KunigiArchive.Infrastructure.Identity;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Diagnostics;

namespace KunigiArchive.Web.Middleware;

// A user signed in with a temporary password (AppClaims.MustChangePassword) is sent to their account page
// until they choose their own password (#24). Only page loads (GET of a Razor component) are redirected:
// forms, the API, static files, media and the sign-out still work.
public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    public const string AccountPath = "/account";

    public Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && context.User.HasClaim(c => c.Type == AppClaims.MustChangePassword)
            && !context.Request.Path.StartsWithSegments(AccountPath)
            && context.Features.Get<IStatusCodeReExecuteFeature>() is null // a 404 for a file stays a 404
            && context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>() is not null)
        {
            context.Response.Redirect(AccountPath);
            return Task.CompletedTask;
        }

        return next(context);
    }
}

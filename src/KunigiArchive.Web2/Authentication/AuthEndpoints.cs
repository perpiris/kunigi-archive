using KunigiArchive.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KunigiArchive.Web2.Authentication;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // The [FromForm] parameter is what makes the antiforgery middleware validate the token.
        endpoints.MapPost("/auth/logout", async (SignInManager<AppUser> signInManager, [FromForm] string? returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return Results.LocalRedirect(LocalUrl.OrHome(returnUrl));
        });

        return endpoints;
    }
}
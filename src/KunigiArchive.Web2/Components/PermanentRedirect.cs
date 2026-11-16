using Microsoft.AspNetCore.Components;

namespace KunigiArchive.Web2.Components;

// A 301 for a team or game whose address changed, so search engines and old links move to the new one.
// NavigateTo would answer 302, so the response is set directly; pages are statically rendered, so it hasn't
// started yet while the page loads. The page then renders nothing of its own.
public static class PermanentRedirect
{
    public static void To(HttpContext? context, NavigationManager navigation, string url)
    {
        if (context is not null && !context.Response.HasStarted)
        {
            context.Response.Redirect(url, permanent: true);
            return;
        }

        navigation.NavigateTo(url);
    }
}

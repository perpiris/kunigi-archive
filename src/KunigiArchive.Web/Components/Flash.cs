namespace KunigiArchive.Web.Components;

// A one-time alert that survives a redirect, like the old app's TempData alerts (AlertMessageHandler):
// a page sets it and navigates away, and the layout (Shared/FlashAlert) shows it once on the next page.
// Kept in a short-lived cookie, so it works for enhanced and full-page navigation alike.
public static class Flash
{
    private const string CookieName = "k_flash";
    private const string SuccessPrefix = "s:";
    private const string ErrorPrefix = "e:";

    public static void Success(HttpContext context, string message)
    {
        Set(context, SuccessPrefix + message);
    }

    public static void Error(HttpContext context, string message)
    {
        Set(context, ErrorPrefix + message);
    }

    // Reads and clears the alert, if there is one.
    public static (string Message, bool IsError)? Take(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out string? value) || string.IsNullOrEmpty(value))
        {
            return null;
        }

        context.Response.Cookies.Delete(CookieName, Options(context));

        string text = Uri.UnescapeDataString(value);
        if (text.StartsWith(ErrorPrefix, StringComparison.Ordinal))
        {
            return (text[ErrorPrefix.Length..], true);
        }

        return text.StartsWith(SuccessPrefix, StringComparison.Ordinal) ? (text[SuccessPrefix.Length..], false) : null;
    }

    private static void Set(HttpContext context, string value)
    {
        context.Response.Cookies.Append(CookieName, Uri.EscapeDataString(value), Options(context));
    }

    private static CookieOptions Options(HttpContext context)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(1),
            IsEssential = true
        };
    }
}

namespace KunigiArchive.Web2.Authentication;

public static class LocalUrl
{
    // Only same-site paths: "//evil.com" and "/\evil.com" are treated by browsers as other hosts.
    public static bool IsLocal(string? url)
    {
        return !string.IsNullOrEmpty(url)
            && url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }

    public static string OrHome(string? url)
    {
        return IsLocal(url) ? url! : "/";
    }
}
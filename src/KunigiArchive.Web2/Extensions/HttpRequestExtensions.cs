namespace KunigiArchive.Web2.Extensions;

public static class HttpRequestExtensions
{
    public static bool IsApiRequest(this HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api");
    }
}
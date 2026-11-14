using KunigiArchive.Infrastructure.Media;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace KunigiArchive.Web2.Extensions;

public static class WebApplicationExtensions
{
    // Stored paths are "media/...", so the media root is served under /media.
    public static WebApplication UseMediaFiles(this WebApplication app)
    {
        string root = app.Services.GetRequiredService<IOptions<MediaStorageOptions>>().Value.RootPath;
        Directory.CreateDirectory(root);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = "/media",
            OnPrepareResponse = context =>
            {
                // File names are random and never reused, so the files can be cached for a long time.
                context.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=2592000";
                context.Context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
            }
        });

        return app;
    }
}
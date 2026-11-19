using System.Xml.Linq;
using KunigiArchive.Application.Sitemap;
using KunigiArchive.Infrastructure.Media;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace KunigiArchive.Web.Extensions;

public static class WebApplicationExtensions
{
    // Beta holds test data: Robots:NoIndex keeps it out of search engines.
    public static WebApplication UseNoIndex(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Robots:NoIndex"))
        {
            app.Use((context, next) =>
            {
                context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
                return next(context);
            });
        }

        return app;
    }

    // robots.txt and sitemap.xml (#27). URLs use the request's scheme and host, which the forwarded headers make
    // the public ones (https://tokinigi.gr). With Robots:NoIndex (a test copy) robots.txt turns everything away.
    public static WebApplication MapSearchEngineFiles(this WebApplication app)
    {
        bool noIndex = app.Configuration.GetValue<bool>("Robots:NoIndex");

        app.MapGet("/robots.txt", (HttpRequest request) =>
        {
            string body = noIndex
                ? "User-agent: *\nDisallow: /\n"
                : "User-agent: *\n" +
                  "Disallow: /auth/\n" +
                  "Disallow: /account\n" +
                  "Disallow: /admin\n" +
                  "Disallow: /users\n" +
                  "Disallow: /my-teams\n" +
                  $"\nSitemap: {request.Scheme}://{request.Host}/sitemap.xml\n";
            return Results.Text(body, "text/plain; charset=utf-8");
        });

        app.MapGet("/sitemap.xml", async (HttpRequest request, GetSitemapHandler handler, CancellationToken cancellationToken) =>
        {
            string root = $"{request.Scheme}://{request.Host}/";
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            IReadOnlyList<string> paths = await handler.HandleAsync(cancellationToken);
            XDocument sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "urlset", paths.Select(path => new XElement(ns + "url", new XElement(ns + "loc", root + path)))));
            return Results.Text(sitemap.Declaration + "\n" + sitemap.Root, "application/xml; charset=utf-8");
        });

        return app;
    }

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
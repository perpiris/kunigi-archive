using KunigiArchive.Web.Data;
using KunigiArchive.Web.Extensions;
using KunigiArchive.Web.Middleware;
using KunigiArchive.Web.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    builder.Services.AddSerilog();
    builder.WebHost.ConfigureKestrel(options =>
        options.Limits.MaxRequestBodySize = ServiceCollectionExtensions.MaxRequestBodyBytes);

    builder.Services
        .AddWebInfrastructure()
        .AddPersistence(builder.Configuration)
        .AddApplicationServices(builder.Configuration);

    WebApplication app = builder.Build();

    // The developer exception page is added automatically in Development.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    app.UseRequestCorrelation();
    app.UseSecurityHeaders();
    app.UseResponseCompression();
    app.UseWebOptimizer();

    const string staticCacheControl = "public, max-age=2592000";
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = staticCacheControl
    });

    string mediaRoot = MediaStorage.ResolveRoot(app.Configuration, app.Environment);
    Directory.CreateDirectory(mediaRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mediaRoot),
        RequestPath = "/media",
        OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = staticCacheControl
    });

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStatusCodePagesWithRedirects("/not-found");
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    using (IServiceScope scope = app.Services.CreateScope())
    {
        DataContext context = scope.ServiceProvider.GetRequiredService<DataContext>();
        await context.Database.MigrateAsync();
    }

    await DataSeed.Seed(app.Services);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // EF design-time tools (dotnet ef) stop the host by throwing HostAbortedException.
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

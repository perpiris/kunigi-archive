using KunigiArchive.Application;
using KunigiArchive.Application.Media;
using KunigiArchive.Infrastructure;
using KunigiArchive.Web.Authentication;
using KunigiArchive.Web.Components;
using KunigiArchive.Web.Extensions;
using KunigiArchive.Web.Middleware;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MediaRules.MaxRequestBytes);

    builder.Services
        .AddSerilogLogging(builder.Configuration, builder.Environment)
        .AddTelemetry(builder.Configuration)
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddHosting(builder.Configuration)
        .AddWebAuthentication()
        .AddWebRateLimiting()
        .AddCompression()
        .AddApi();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    WebApplication app = builder.Build();

    // First, so everything after it (logging, rate limits, HTTPS redirect, cookies) sees the client's scheme and IP.
    app.UseForwardedHeaders();

    // Before the request log, so its line carries the request's id too.
    app.UseMiddleware<RequestCorrelationMiddleware>();

    // Outermost after that, so it logs the final status code after errors have been turned into responses.
    app.UseSerilogRequestLogging();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseNoIndex();
    app.UseResponseCompression();

    // No UseHsts: nginx-proxy sends Strict-Transport-Security, and a second header from here only disagreed with it.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    app.UseWhen(context => context.Request.IsApiRequest(), api =>
    {
        api.UseExceptionHandler();
        api.UseStatusCodePages();
    });
    app.UseWhen(context => !context.Request.IsApiRequest(), pages =>
        pages.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

    // No UseHttpsRedirection: nginx-proxy redirects http to https, and the container only listens on http (8080).
    app.UseMediaFiles();

    // Explicit, so routing runs after the status code pages: a re-executed /not-found has to be routed again,
    // otherwise unknown URLs get an empty 404.
    app.UseRouting();

    // After routing: the limits are per endpoint (the auth pages carry [EnableRateLimiting]).
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<MustChangePasswordMiddleware>();
    app.UseAntiforgery();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Kunigi Archive API"));
    }

    app.MapStaticAssets();
    app.MapSearchEngineFiles();
    app.MapLegacyRedirects();
    // 200 "Healthy" when the database answers, 503 otherwise (#39). Used by the deploy's post-check.
    app.MapHealthChecks("/healthz");
    app.MapControllers();
    app.MapAuthEndpoints();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await app.Services.InitializeDatabaseAsync();
    await app.RunAsync();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // EF design-time tools (dotnet ef) stop the host by throwing HostAbortedException.
    Log.Fatal(exception, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
using KunigiArchive.Application;
using KunigiArchive.Infrastructure;
using KunigiArchive.Web2.Components;
using KunigiArchive.Web2.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services
        .AddSerilogLogging(builder.Configuration)
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApi();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    WebApplication app = builder.Build();

    // Outermost, so it logs the final status code after errors have been turned into responses.
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseWhen(context => context.Request.IsApiRequest(), api =>
    {
        api.UseExceptionHandler();
        api.UseStatusCodePages();
    });
    app.UseWhen(context => !context.Request.IsApiRequest(), pages =>
        pages.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

    app.UseHttpsRedirection();
    app.UseAntiforgery();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Kunigi Archive API"));
    }

    app.MapStaticAssets();
    app.MapControllers();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await app.Services.MigrateDatabaseAsync();
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
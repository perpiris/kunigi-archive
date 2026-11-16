// Imports the old site (KunigiArchive + its media folder) into Web2 (KunigiArchiveV2 + its media folder).
// Re-runnable: each run replaces Web2's content with a fresh copy; ids stay the same (import.IdMap).
//
//   dotnet run --project src/KunigiArchive.Import -- \
//       --ConnectionStrings:Source="..." --ConnectionStrings:KunigiArchive="..." \
//       --SourceMedia:RootPath=/path/to/kunigi_media --MediaStorage:RootPath=/path/to/kunigi_media_v2 [--apply]
//
// Without --apply it's a dry run: everything is checked and reported, nothing is written. The old database and
// media folder are only ever read. The report is printed and saved as import-report.txt.

using KunigiArchive.Application.Abstractions;
using KunigiArchive.Application.Common;
using KunigiArchive.Application.Media;
using KunigiArchive.Import;
using KunigiArchive.Infrastructure;
using KunigiArchive.Infrastructure.Media;
using KunigiArchive.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

bool apply = args.Contains("--apply", StringComparer.Ordinal);
string[] configArgs = args.Where(a => a != "--apply").ToArray();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = configArgs,
    ContentRootPath = AppContext.BaseDirectory,
    EnvironmentName = Environments.Production
});
builder.Configuration.AddCommandLine(configArgs);

builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// Identity's token providers need it; nothing the import does uses the keys.
builder.Services.AddDataProtection();

using IHost host = builder.Build();

string source = builder.Configuration.GetConnectionString("Source") ?? string.Empty;
string target = builder.Configuration.GetConnectionString(DependencyInjection.ConnectionStringName) ?? string.Empty;
string sourceMedia = builder.Configuration["SourceMedia:RootPath"] ?? string.Empty;
string targetMedia = host.Services.GetRequiredService<IOptions<MediaStorageOptions>>().Value.RootPath;

if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(sourceMedia))
{
    Console.Error.WriteLine("Set ConnectionStrings:Source and SourceMedia:RootPath (see Program.cs).");
    return 2;
}

sourceMedia = Path.GetFullPath(sourceMedia).TrimEnd(Path.DirectorySeparatorChar);
string targetMediaFull = Path.GetFullPath(targetMedia).TrimEnd(Path.DirectorySeparatorChar);

// The import replaces the target's content: it must never be pointed at the live site's data.
string sourceDatabase = new SqlConnectionStringBuilder(source).InitialCatalog;
string targetDatabase = new SqlConnectionStringBuilder(target).InitialCatalog;
if (string.Equals(sourceDatabase, targetDatabase, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Source and target are the same database ('{sourceDatabase}'). Refusing to run.");
    return 2;
}

if (IsSameOrInside(targetMediaFull, sourceMedia) || IsSameOrInside(sourceMedia, targetMediaFull))
{
    Console.Error.WriteLine("The target media folder is (or contains, or is inside) the old media folder. Refusing to run.");
    return 2;
}

if (!Directory.Exists(sourceMedia))
{
    Console.Error.WriteLine($"The old media folder '{sourceMedia}' doesn't exist.");
    return 2;
}

Console.WriteLine(apply
    ? $"Importing {sourceDatabase} → {targetDatabase}, media {sourceMedia} → {targetMediaFull}. Web2's content will be replaced."
    : $"Dry run: {sourceDatabase} → {targetDatabase}. Nothing will be written (add --apply to import).");

// Brings the new schema up to date and makes sure the roles exist.
await host.Services.InitializeDatabaseAsync();

ImportReport report = new ImportReport();
using (IServiceScope scope = host.Services.CreateScope())
{
    ImportPipeline pipeline = new ImportPipeline(
        scope.ServiceProvider.GetRequiredService<KunigiArchiveDbContext>(),
        scope.ServiceProvider.GetRequiredService<IMediaStorage>(),
        scope.ServiceProvider.GetRequiredService<IRichTextSanitizer>(),
        scope.ServiceProvider.GetRequiredService<ISlugHistory>(),
        report,
        new ImportSettings(apply, source, sourceMedia, targetMediaFull));

    try
    {
        await pipeline.RunAsync(CancellationToken.None);
    }
    catch (Exception exception)
    {
        report.Info($"FAILED, nothing was committed: {exception}");
        Console.Error.WriteLine(report.Render());
        await File.WriteAllTextAsync("import-report.txt", report.Render());
        return 1;
    }
}

string rendered = report.Render();
Console.WriteLine(rendered);
await File.WriteAllTextAsync("import-report.txt", rendered);
return 0;

static bool IsSameOrInside(string path, string folder)
{
    return string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}

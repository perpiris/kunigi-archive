using System.IO.Compression;
using System.Threading.RateLimiting;
using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Middleware;
using KunigiArchive.Web.Services;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 250 * 1024 * 1024; // 250 MB total request size
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB per file
    options.ValueCountLimit = 4096; // Support higher number of uploaded files/keys
});

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.SmallestSize);

builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.MinifyJsFiles("js/**/*.js");
    pipeline.MinifyCssFiles("css/**/*.css", "lib/bootstrap/dist/css/**/*.css",
        "lib/fancybox/**/*.css", "lib/quill/**/*.css");
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IDirectoryService, DirectoryService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IPuzzleService, PuzzleService>();
builder.Services.AddScoped<IPostService, PostService>();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, AppRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/";
    options.LoginPath = "/auth/login";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var mediaPath = builder.Configuration["MediaDirectory"];
if (string.IsNullOrEmpty(mediaPath))
{
    mediaPath = Path.Combine(builder.Environment.ContentRootPath, "..", "kunigi_media");
}
if (!Directory.Exists(mediaPath))
{
    Directory.CreateDirectory(mediaPath);
}

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseWebOptimizer();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public, max-age=2592000";
    }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaPath),
    RequestPath = "/media",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public, max-age=2592000";
    }
});
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStatusCodePagesWithRedirects("/not-found");
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
        "default",
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();

    if (context.Database.GetPendingMigrations().Any())
    {
        context.Database.Migrate();
    }

    await DataSeed.Seed(app.Services);

    // var directoryService = scope.ServiceProvider.GetRequiredService<IDirectoryService>();
    // await directoryService.GenerateMissingThumbnailsAsync();
}

app.Run();
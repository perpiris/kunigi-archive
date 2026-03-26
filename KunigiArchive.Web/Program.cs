using KunigiArchive.Web.Data;
using KunigiArchive.Web.Entities;
using KunigiArchive.Web.Services;
using KunigiArchive.Web.Services.Interfaces;
using KunigiArchive.Web.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IDirectoryService, DirectoryService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IPuzzleService, PuzzleService>();
builder.Services.AddScoped<IPostService, PostService>();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, AppRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequiredUniqueChars = 0;
    })
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/";
    options.LoginPath = "/login";
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

var mediaPath = Path.Combine(builder.Environment.WebRootPath, "media");
if (!Directory.Exists(mediaPath))
{
    Directory.CreateDirectory(mediaPath);
}

app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStatusCodePagesWithRedirects("/not-found");
app.UseRouting();
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
}

app.Run();
using AspNet.Security.OAuth.GitHub;
using GalleryApp.Data;
using GalleryApp.Models;
using GalleryApp.Services.Images;
using GalleryApp.Services.Logging;
using GalleryApp.Services.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity.UI.Services;
using GalleryApp.Services.Aspects;
using GalleryApp.Services.Logging.Commands;
using GalleryApp.Services.Photos;
using GalleryApp.Services.Metrics;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContextPool<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var authBuilder = builder.Services.AddAuthentication();

var googleId = builder.Configuration["Authentication:Google:ClientId"];
var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleId;
        options.ClientSecret = googleSecret;
    });
}

var githubId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrWhiteSpace(githubId) && !string.IsNullOrWhiteSpace(githubSecret))
{
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = githubId;
        options.ClientSecret = githubSecret;
        options.Scope.Add("user:email");
    });
}
builder.Services.AddAspects();

builder.Services.AddProxiedScoped<IImageProcessor, ImageSharpProcessor>();
builder.Services.AddSingleton<IEmailSender, GalleryApp.Services.Email.DevEmailSender>();

builder.Services.Configure<LocalStorageOptions>(builder.Configuration.GetSection("Storage:Local"));
builder.Services.Configure<MinioStorageOptions>(builder.Configuration.GetSection("Storage:Minio"));

builder.Services.AddSingleton<LocalStorageService>();
builder.Services.AddSingleton<MinioStorageService>();

builder.Services.AddSingleton<StorageSelectorService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddProxiedScoped<IActionLogger, ActionLogger>();

builder.Services.AddScoped<ActionCommandDispatcher>();

builder.Services.AddScoped<IStorageService>(sp =>
{
    var selector = sp.GetRequiredService<StorageSelectorService>();
    var logger = sp.GetRequiredService<IActionLogger>();
    return new LoggingStorageDecorator(selector, logger);
});

builder.Services.AddProxiedScoped<IPhotoFacade, PhotoFacade>();

builder.Services.AddMetrics();
builder.Services.AddSingleton<GalleryMetrics>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(GalleryMetrics.MeterName)        
            .AddAspNetCoreInstrumentation()            
            .AddRuntimeInstrumentation()               
            .AddPrometheusExporter();
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.Services.GetRequiredService<GalleryMetrics>();

app.MapPrometheusScrapingEndpoint();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsEnvironment("Testing"))
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var provider = cfg["Storage:Provider"];

    if (string.Equals(provider, "Minio", StringComparison.OrdinalIgnoreCase))
    {
        var minio = scope.ServiceProvider.GetRequiredService<MinioStorageService>();
        await minio.EnsureBucketAsync();
    }
}

await IdentitySeed.SeedAsync(app.Services, app.Configuration);

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

public partial class Program { }

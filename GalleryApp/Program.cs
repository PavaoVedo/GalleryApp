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

using GalleryApp.Services.Logging.Commands;
using GalleryApp.Services.Photos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
        options.Scope.Add("user:email");
    });

builder.Services.AddScoped<IImageProcessor, ImageSharpProcessor>();
builder.Services.AddSingleton<IEmailSender, GalleryApp.Services.Email.DevEmailSender>();

builder.Services.Configure<LocalStorageOptions>(builder.Configuration.GetSection("Storage:Local"));
builder.Services.Configure<MinioStorageOptions>(builder.Configuration.GetSection("Storage:Minio"));

builder.Services.AddSingleton<LocalStorageService>();
builder.Services.AddSingleton<MinioStorageService>();

builder.Services.AddSingleton<StorageSelectorService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActionLogger, ActionLogger>();

builder.Services.AddScoped<ActionCommandDispatcher>();

builder.Services.AddScoped<IStorageService>(sp =>
{
    var selector = sp.GetRequiredService<StorageSelectorService>();
    var logger = sp.GetRequiredService<IActionLogger>();
    return new LoggingStorageDecorator(selector, logger);
});

builder.Services.AddScoped<PhotoFacade>();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var provider = cfg["Storage:Provider"];

    if (string.Equals(provider, "Minio", StringComparison.OrdinalIgnoreCase))
    {
        var minio = scope.ServiceProvider.GetRequiredService<MinioStorageService>();
        await minio.EnsureBucketAsync();
    }
}

await IdentitySeed.SeedAsync(app.Services, app.Configuration);

if (!app.Environment.IsDevelopment())
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

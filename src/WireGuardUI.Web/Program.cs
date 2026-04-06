using System.Runtime.InteropServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Core.Services;
using WireGuardUI.Infrastructure.Crypto;
using WireGuardUI.Infrastructure.Data;
using WireGuardUI.Infrastructure.Email;
using WireGuardUI.Infrastructure.Repositories;
using WireGuardUI.Infrastructure.WireGuard;
using WireGuardUI.Web;

var builder = WebApplication.CreateBuilder(args);

// Settings
var settings = builder.Configuration.GetSection("WireGuardUI").Get<WireGuardUiSettings>()
    ?? new WireGuardUiSettings();
builder.Services.AddSingleton(settings);

// Database
var dbDir = Path.GetDirectoryName(settings.ResolvedDbPath);
// If develop use a local SQLite database in the project directory, otherwise use a system-wide location
if (builder.Environment.IsDevelopment())
{
    dbDir = Path.Combine(builder.Environment.ContentRootPath, "db");
}
else if (string.IsNullOrEmpty(dbDir))
{
    dbDir = Path.Combine(Path.GetTempPath(), "WireGuardUI");
}
if (!string.IsNullOrEmpty(dbDir))
{
    Directory.CreateDirectory(dbDir);
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"DataSource={settings.ResolvedDbPath}"));

// Repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IServerConfigRepository, ServerConfigRepository>();
builder.Services.AddScoped<IGlobalSettingRepository, GlobalSettingRepository>();
builder.Services.AddScoped<IEmailSettingRepository, EmailSettingRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();

// Services
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IKeyPairService, BouncyCastleKeyPairService>();
builder.Services.AddSingleton<IIpAllocationService, IpAllocationService>();

// Platform-specific WireGuard service
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.Services.AddScoped<IWireGuardService>(sp => new WindowsWireGuardService(
        sp.GetRequiredService<IClientRepository>(),
        sp.GetRequiredService<IServerConfigRepository>(),
        sp.GetRequiredService<IGlobalSettingRepository>(),
        settings.ResolvedWgExePath));
else if (builder.Environment.IsDevelopment() || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    builder.Services.AddScoped<IWireGuardService, NoOpWireGuardService>();
else
    builder.Services.AddScoped<IWireGuardService, LinuxWireGuardService>();

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(settings.SessionExpiryHours);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Blazor + MudBlazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Migrate and seed DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed admin user if none exists
    if (!db.AdminUsers.Any())
    {
        var seedUsername = settings.DefaultUsername?.Trim() ?? string.Empty;
        var seedPassword = settings.DefaultPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(seedUsername) || string.IsNullOrWhiteSpace(seedPassword))
        {
            if (app.Environment.IsDevelopment())
            {
                seedUsername = "admin";
                seedPassword = "admin";
            }
            else
            {
                throw new InvalidOperationException(
                    "No admin user exists. Configure WireGuardUI:DefaultUsername and WireGuardUI:DefaultPassword with strong values to bootstrap the first admin account.");
            }
        }

        if (!app.Environment.IsDevelopment() &&
            seedUsername.Equals("admin", StringComparison.OrdinalIgnoreCase) &&
            seedPassword == "admin")
        {
            throw new InvalidOperationException(
                "Refusing to bootstrap with default admin/admin credentials outside Development.");
        }

        db.AdminUsers.Add(new AdminUser
        {
            Username = seedUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword)
        });
        db.SaveChanges();
    }

    // Seed global settings if none exists
    if (!db.GlobalSettings.Any())
    {
        db.GlobalSettings.Add(new GlobalSetting
        {
            ConfigFilePath = settings.ResolvedConfigPath
        });
        db.SaveChanges();
    }
}

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Auth endpoints (cookies must be written by non-Blazor endpoints)
app.MapPost("/account/login", async (HttpContext ctx, IAdminUserRepository userRepo,
    string? returnUrl) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var user = await userRepo.GetAsync();
    if (user.Username != username || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        return Results.Redirect("/login?error=1");

    var claims = new[] { new Claim(ClaimTypes.Name, user.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect(NormalizeLocalReturnUrl(returnUrl));
});

app.MapPost("/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<WireGuardUI.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string NormalizeLocalReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
        return "/";

    if (!returnUrl.StartsWith('/'))
        return "/";

    if (returnUrl.StartsWith("//", StringComparison.Ordinal) ||
        returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        return "/";

    return Uri.TryCreate(returnUrl, UriKind.Relative, out _)
        ? returnUrl
        : "/";
}

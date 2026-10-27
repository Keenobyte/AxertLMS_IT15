using System;
using AxertLMS.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using AxertLMS.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// FORWARDED HEADERS CONFIGURATION
// ============================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});

// ============================================================
// MVC
// ============================================================
builder.Services.AddControllersWithViews();

// ============================================================
// APPLICATION SERVICES
// ============================================================
builder.Services.AddScoped<
    AxertLMS.Services.InternalPlagiarismService>();
builder.Services.AddScoped<
    AxertLMS.Services.InternalQuizGeneratorService>();

// ============================================================
// GOOGLE OAUTH + CALENDAR
// ============================================================
builder.Services
    .AddOptions<GoogleOAuthOptions>()
    .Bind(builder.Configuration.GetSection("GoogleOAuth"));

builder.Services.AddScoped<GoogleOAuthService>();
builder.Services.AddScoped<GoogleTokenStore>();
builder.Services.AddScoped<GoogleCalendarService>();

// ============================================================
// DATABASE
// ============================================================
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));

// ============================================================
// HTTP CLIENT
// Used for Google's token and userinfo endpoints.
// ============================================================
builder.Services.AddHttpClient(
    "Google",
    client =>
    {
        client.Timeout =
            TimeSpan.FromSeconds(30);

        client.DefaultRequestHeaders
            .UserAgent.ParseAdd(
                "AxertLMS/1.0");
    });

// ============================================================
// DATA PROTECTION
// Used to encrypt Google OAuth tokens before database storage.
// ============================================================
var keyRingSetting =
    builder.Configuration["DataProtection:KeyRingPath"];

string keyRingPath =
    string.IsNullOrWhiteSpace(keyRingSetting)
        ? Path.Combine(
            builder.Environment.ContentRootPath,
            "App_Data",
            "DataProtectionKeys")
        : (Path.IsPathRooted(keyRingSetting)
            ? keyRingSetting
            : Path.Combine(
                builder.Environment.ContentRootPath,
                keyRingSetting));

Directory.CreateDirectory(keyRingPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(keyRingPath))
    .SetApplicationName("AxertLMS");

// ============================================================
// SESSION
// ============================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(
    options =>
    {
        options.IdleTimeout =
            TimeSpan.FromMinutes(60);

        options.Cookie.Name =
            "AxertLMS.Session";

        options.Cookie.HttpOnly =
            true;

        options.Cookie.IsEssential =
            true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;

        options.Cookie.SameSite =
            SameSiteMode.Lax;
    });

// ============================================================
// ANTIFORGERY
// ============================================================
builder.Services.AddAntiforgery(
    options =>
    {
        options.Cookie.Name =
            "AxertLMS.AntiForgery";

        options.Cookie.HttpOnly =
            true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.SameAsRequest;

        options.Cookie.SameSite =
            SameSiteMode.Strict;
    });

// ============================================================
// BUILD
// ============================================================
var app =
    builder.Build();

// ============================================================
// FORWARDED HEADERS MIDDLEWARE
// Must be invoked early in the request pipeline
// ============================================================
app.UseForwardedHeaders();

// ============================================================
// ERROR HANDLING
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}

// ============================================================
// SECURITY HEADERS
// ============================================================
app.Use(
    async (context, next) =>
    {
        context.Response.Headers.Append(
            "X-Frame-Options",
            "DENY");

        context.Response.Headers.Append(
            "X-Content-Type-Options",
            "nosniff");

        context.Response.Headers.Append(
            "Referrer-Policy",
            "strict-origin-when-cross-origin");

        context.Response.Headers.Append(
            "Content-Security-Policy",
            "frame-ancestors 'none'");

        await next();
    });

// ============================================================
// HTTP PIPELINE
// ============================================================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// ============================================================
// DEFAULT MVC ROUTE
// ============================================================
app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Login}/{id?}");

app.Run();
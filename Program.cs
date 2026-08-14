using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using ReskinManager.Services;

const int Port = 5180;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://localhost:{Port}");
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<AddonService>();
builder.Services.AddSingleton<BindsService>();
builder.Services.AddSingleton<OptimizationService>();
builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<UpdateService>();
var app = builder.Build();

try
{
    app.Services.GetRequiredService<BindsService>().ApplyToCfg();
    app.Services.GetRequiredService<OptimizationService>().Ensure();
}
catch { }

var reskinsRoot = Path.Combine(app.Environment.ContentRootPath, "addons-src");
if (Directory.Exists(reskinsRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(reskinsRoot),
        RequestPath = "/reskins"
    });
}
// Запрет кэширования в WebView2: статика всегда должна отдаваться заново.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    ctx.Response.Headers.Pragma = "no-cache";
    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles();

// CSRF-защита: изменяющие запросы разрешены только с того же origin (WebView).
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
    {
        var origin = ctx.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            var allowed = Uri.TryCreate(origin, UriKind.Absolute, out var o)
                && o.Scheme == "http"
                && (o.Host == "localhost" || o.Host == "127.0.0.1" || o.Host == "[::1]")
                && o.Port == Port;
            if (!allowed)
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }
    }

    await next();
});

app.MapGet("/api/addons", async (AddonService svc) => await svc.GetAddons());

app.MapGet("/api/update", async (UpdateService updater, bool force) =>
{
    var info = await updater.CheckForUpdateAsync(force);
    return Results.Json(new
    {
        info.HasUpdate,
        info.CurrentVersion,
        info.LatestVersion,
        info.ReleaseName,
        info.ReleaseNotes,
        info.AssetUrl,
        error = updater.LastError
    });
});

app.MapGet("/api/update/apply", async (UpdateService updater) =>
    await updater.PrepareAndApplyAsync());

app.MapPost("/api/update/restart", (UpdateService updater, [FromBody] string script) =>
{
    if (!string.IsNullOrWhiteSpace(script) && File.Exists(script))
    {
        UpdateService.RestartNow(script);
        return Results.Ok(new { ok = true });
    }
    return Results.Ok(new { ok = false, message = "Скрипт обновления не найден." });
});

app.MapGet("/api/settings", (AddonService svc, SettingsService settings) => new
{
    configuredPath = settings.Current.AddonsTargetPath,
    targetPath = svc.TargetBase,
    autoDetected = SteamLocator.FindGmodAddonsPath(),
    error = svc.TargetExistsError(),
    accentColor = settings.Current.AccentColor
});

app.MapGet("/api/settings/detect", () => new
{
    path = SteamLocator.FindGmodAddonsPath()
});

app.MapPost("/api/settings", (SettingsService settings, SettingsUpdateDto dto) =>
{
    var current = settings.Current;
    settings.Save(new SettingsData
    {
        AddonsTargetPath = dto.TargetPath ?? current.AddonsTargetPath,
        AccentColor = dto.AccentColor ?? current.AccentColor
    });
    return Results.Ok(new
    {
        ok = true,
        targetPath = settings.Current.AddonsTargetPath,
        accentColor = settings.Current.AccentColor
    });
});

app.MapPost("/api/addons/{id}/install", async (string id, AddonService svc) =>
{
    var result = await svc.InstallAsync(id);
    return Results.Ok(new { ok = result.Ok, message = result.Message, path = result.Data });
});

app.MapPost("/api/addons/{id}/uninstall", (string id, AddonService svc) =>
{
    var result = svc.UninstallAsync(id);
    return Results.Ok(new { ok = result.Ok, message = result.Message, path = result.Data });
});

// ---------- binds ----------

app.MapGet("/api/binds", (BindsService binds) =>
{
    var cfg = binds.CfgDir();
    var written = cfg != null && File.Exists(Path.Combine(cfg, "adonis_binds.cfg"));
    return Results.Ok(new { binds = binds.Current, gmodPath = cfg, written });
});

app.MapPost("/api/binds", (BindsService binds, List<BindEntry> list) =>
{
    var result = binds.Save(list ?? []);
    return Results.Ok(new { ok = result.Ok, message = result.Message });
});

// ---------- game optimization ----------

app.MapGet("/api/game/optimization", (OptimizationService opt) =>
{
    var enabled = opt.EnabledKeys();
    return Results.Ok(new
    {
        applied = opt.IsApplied(),
        path = opt.CfgDir(),
        options = OptimizationService.Options.Select(o => new
        {
            key = o.Key,
            title = o.Title,
            description = o.Description,
            commands = o.Commands,
            enabled = enabled.Contains(o.Key, StringComparer.OrdinalIgnoreCase)
        })
    });
});

app.MapPost("/api/game/optimization", (OptimizationService opt, OptimizationUpdateDto dto) =>
{
    var result = dto.Enabled ? opt.Apply() : opt.Remove();
    return Results.Ok(new { ok = result.Ok, message = result.Message, applied = opt.IsApplied() });
});

app.MapPost("/api/game/optimization/option", (OptimizationService opt, OptimizationOptionUpdateDto dto) =>
{
    var valid = OptimizationService.Options.Any(o => o.Key.Equals(dto.Key, StringComparison.OrdinalIgnoreCase));
    if (!valid)
        return Results.Ok(new { ok = false, message = "Неизвестный вариант оптимизации" });

    var keys = opt.EnabledKeys();
    if (dto.Enabled)
        keys.Add(dto.Key);
    else
        keys.RemoveAll(k => k.Equals(dto.Key, StringComparison.OrdinalIgnoreCase));
    opt.SaveEnabledKeys(keys);

    var applied = opt.IsApplied();
    var message = dto.Enabled ? "Вариант включён" : "Вариант выключен";
    if (applied)
    {
        var res = opt.Apply();
        applied = res.Ok && opt.IsApplied();
        if (!res.Ok) message = res.Message;
    }

    return Results.Ok(new
    {
        ok = true,
        message,
        applied,
        enabled = keys.Any(k => k.Equals(dto.Key, StringComparison.OrdinalIgnoreCase))
    });
});

// ---------- auth ----------
app.MapGet("/api/auth/status", (AuthService auth, HttpContext ctx) =>
{
    var user = auth.GetUser(GetSessionToken(ctx));
    return Results.Ok(new
    {
        authenticated = user != null,
        user,
        discordConfigured = auth.DiscordConfigured,
        guest = ctx.Request.Cookies["reskin_guest"] == "1"
    });
});

app.MapPost("/api/auth/guest", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Append("reskin_guest", "1", new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = TimeSpan.FromDays(365)
    });
    return Results.Ok(new { ok = true });
});

app.MapGet("/api/auth/discord/begin", (AuthService auth) =>
{
    if (!auth.DiscordConfigured) return Results.Json(new { url = "", error = "Discord не настроен" });
    var (url, loginId) = auth.BeginDiscordLogin();
    return Results.Json(new { url, loginId });
});

app.MapGet("/api/auth/discord/callback", async (AuthService auth, HttpContext ctx) =>
{
    var state = ctx.Request.Query["state"].ToString();
    if (string.IsNullOrEmpty(state) || !auth.HasPending(state))
        return Results.Redirect("/api/auth/complete?error=1");

    var code = ctx.Request.Query["code"].ToString();
    if (string.IsNullOrEmpty(code))
    {
        auth.CompleteDiscordLogin(state, null, "no_code");
        return Results.Redirect("/api/auth/complete?error=1");
    }

    var user = await auth.ExchangeDiscordCodeAsync(code);
    if (user is null)
    {
        auth.CompleteDiscordLogin(state, null, "exchange_failed");
        return Results.Redirect("/api/auth/complete?error=1");
    }

    var token = auth.CreateSession(user);
    auth.CompleteDiscordLogin(state, token, null);
    return Results.Redirect("/api/auth/complete");
});

app.MapGet("/api/auth/discord/poll", (AuthService auth, HttpContext ctx, string loginId) =>
{
    var (token, error, waiting) = auth.PollDiscordLogin(loginId);
    if (token is not null)
    {
        ctx.Response.Cookies.Delete("reskin_guest", new CookieOptions { Path = "/" });
        ctx.Response.Cookies.Append("reskin_session", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(365)
        });
        return Results.Json(new { authenticated = true, waiting = false });
    }

    return Results.Json(new { authenticated = false, error, waiting });
});

app.MapGet("/api/auth/complete", () => Results.Content(
    """
    <!DOCTYPE html>
    <html lang="ru">
    <head><meta charset="utf-8"><title>Adonis</title>
    <style>
      body{font-family:"Segoe UI",sans-serif;background:#0b0b0d;color:#e8e8ec;
           display:grid;place-items:center;height:100vh;margin:0}
      div{text-align:center}p{color:#8c8c96}
    </style></head>
    <body><div>
      <h2>Вход выполнен</h2>
      <p>Можно закрыть эту вкладку и вернуться в Adonis</p>
    </div></body></html>
    """,
    "text/html; charset=utf-8"));

app.MapPost("/api/auth/logout", (AuthService auth, HttpContext ctx) =>
{
    auth.RemoveSession(GetSessionToken(ctx));
    ctx.Response.Cookies.Delete("reskin_session", new CookieOptions { Path = "/" });
    ctx.Response.Cookies.Delete("reskin_guest", new CookieOptions { Path = "/" });
    return Results.Ok(new { ok = true });
});

await app.StartAsync();

var url = app.Services.GetRequiredService<IServer>()
    .Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
    ?? $"http://localhost:{Port}";

app.Services.GetRequiredService<AppState>().RedirectBase = url;

var exited = new ManualResetEventSlim(false);
var uiThread = new Thread(() =>
{
    ReskinManager.DesktopApp.Run(url);
    exited.Set();
});
uiThread.SetApartmentState(ApartmentState.STA);
uiThread.Start();

exited.Wait();

await app.StopAsync();

static string? GetSessionToken(HttpContext ctx) => ctx.Request.Cookies["reskin_session"];

public sealed record SettingsUpdateDto(string? TargetPath, string? AccentColor);

public sealed record OptimizationUpdateDto(bool Enabled);
public sealed record OptimizationOptionUpdateDto(string Key, bool Enabled);

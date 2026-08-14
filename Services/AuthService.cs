using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReskinManager.Services;

public sealed record UserInfo(string Provider, string ProviderId, string Name, string AvatarUrl);

public sealed class DiscordAuthConfig
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}

public sealed class AuthConfig
{
    public DiscordAuthConfig Discord { get; set; } = new();
}

public sealed class AppState
{
    public string RedirectBase { get; set; } = "http://localhost:5180";
}

public sealed class AuthService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly AuthConfig _config;
    private readonly AppState _state;
    private readonly string _sessionsPath;
    private readonly object _saveLock = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly ConcurrentDictionary<string, UserInfo> _sessions = new();

    public bool DiscordConfigured =>
        !string.IsNullOrWhiteSpace(_config.Discord.ClientId) &&
        !string.IsNullOrWhiteSpace(_config.Discord.ClientSecret);

    public AuthService(IWebHostEnvironment env, AppState state)
    {
        _state = state;
        _config = LoadConfig(Path.Combine(env.ContentRootPath, "auth.json"));
        _sessionsPath = Path.Combine(env.ContentRootPath, "sessions.json");
        LoadSessions();
    }

    public string BuildDiscordLoginUrl(string state)
    {
        var callback = $"{_state.RedirectBase}/api/auth/discord/callback";
        return "https://discord.com/api/oauth2/authorize?" + string.Join("&", new[]
        {
            "client_id=" + Uri.EscapeDataString(_config.Discord.ClientId),
            "response_type=code",
            "redirect_uri=" + Uri.EscapeDataString(callback),
            "scope=identify",
            "state=" + Uri.EscapeDataString(state)
        });
    }

    public async Task<UserInfo?> ExchangeDiscordCodeAsync(string code)
    {
        var callback = $"{_state.RedirectBase}/api/auth/discord/callback";
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _config.Discord.ClientId,
            ["client_secret"] = _config.Discord.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = callback
        };

        HttpResponseMessage tokenRes;
        try
        {
            tokenRes = await _http.PostAsync("https://discord.com/api/oauth2/token", new FormUrlEncodedContent(form));
        }
        catch
        {
            return null;
        }

        if (!tokenRes.IsSuccessStatusCode) return null;

        using var tokenDoc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();
        if (string.IsNullOrEmpty(accessToken)) return null;

        using var req = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage userRes;
        try
        {
            userRes = await _http.SendAsync(req);
        }
        catch
        {
            return null;
        }

        if (!userRes.IsSuccessStatusCode) return null;

        using var userDoc = JsonDocument.Parse(await userRes.Content.ReadAsStringAsync());
        var root = userDoc.RootElement;
        var id = root.GetProperty("id").GetString() ?? "";
        var name = (root.TryGetProperty("global_name", out var g) && g.ValueKind == JsonValueKind.String
            ? g.GetString()
            : null) ?? (root.TryGetProperty("username", out var u) ? u.GetString() : null) ?? id;
        var avatarHash = root.TryGetProperty("avatar", out var av) && av.ValueKind == JsonValueKind.String
            ? av.GetString()
            : null;
        var avatar = "";
        if (!string.IsNullOrEmpty(avatarHash))
        {
            var ext = avatarHash.StartsWith("a_", StringComparison.Ordinal) ? ".gif" : ".png";
            avatar = $"https://cdn.discordapp.com/avatars/{id}/{avatarHash}{ext}?size=128";
        }
        else
        {
            var idx = ulong.TryParse(id, out var num) ? (num >> 22) % 6 : 0UL;
            avatar = $"https://cdn.discordapp.com/embed/avatars/{idx}.png";
        }

        return new UserInfo("discord", id, name, avatar);
    }

    public string CreateSession(UserInfo user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        _sessions[HashToken(token)] = user;
        SaveSessions();
        return token;
    }

    public UserInfo? GetUser(string? token) =>
        token is null ? null : _sessions.TryGetValue(HashToken(token), out var user) ? user : null;

    public void RemoveSession(string? token)
    {
        if (token is null || !_sessions.TryRemove(HashToken(token), out _)) return;
        SaveSessions();
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private void LoadSessions()
    {
        try
        {
            if (!File.Exists(_sessionsPath)) return;
            var data = JsonSerializer.Deserialize<Dictionary<string, UserInfo>>(File.ReadAllText(_sessionsPath), JsonOpts);
            if (data is null) return;
            foreach (var kv in data) _sessions[kv.Key] = kv.Value;
        }
        catch
        {
        }
    }

    private void SaveSessions()
    {
        try
        {
            lock (_saveLock)
            {
                var dir = Path.GetDirectoryName(_sessionsPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_sessionsPath, JsonSerializer.Serialize(_sessions));
            }
        }
        catch
        {
        }
    }

    // ---------- browser handoff ----------

    public sealed class PendingLogin
    {
        public string State { get; set; } = "";
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string? Token { get; set; }
        public string? Error { get; set; }
    }

    private readonly ConcurrentDictionary<string, PendingLogin> _pendingLogins = new();

    public (string url, string loginId) BeginDiscordLogin()
    {
        var loginId = Guid.NewGuid().ToString("N");
        var state = Guid.NewGuid().ToString("N");
        PrunePending();
        _pendingLogins[loginId] = new PendingLogin { State = state };
        return (BuildDiscordLoginUrl(state), loginId);
    }

    public bool HasPending(string state)
    {
        foreach (var kv in _pendingLogins)
            if (kv.Value.State == state) return true;
        return false;
    }

    public void CompleteDiscordLogin(string state, string? token, string? error)
    {
        foreach (var kv in _pendingLogins)
        {
            if (kv.Value.State != state) continue;
            kv.Value.Token = token;
            kv.Value.Error = error;
            return;
        }
    }

    public (string? token, string? error, bool waiting) PollDiscordLogin(string loginId)
    {
        if (_pendingLogins.TryGetValue(loginId, out var pending))
        {
            if (pending.Token is not null)
            {
                _pendingLogins.TryRemove(loginId, out _);
                return (pending.Token, null, false);
            }

            if (pending.Error is not null)
            {
                _pendingLogins.TryRemove(loginId, out _);
                return (null, pending.Error, false);
            }

            return (null, null, true);
        }

        return (null, "not_found", false);
    }

    private void PrunePending()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        foreach (var kv in _pendingLogins)
            if (kv.Value.CreatedAt < cutoff) _pendingLogins.TryRemove(kv.Key, out _);
    }

    private static AuthConfig LoadConfig(string path)
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<AuthConfig>(File.ReadAllText(path), JsonOpts) ?? new AuthConfig();
        }
        catch
        {
        }

        return new AuthConfig();
    }
}

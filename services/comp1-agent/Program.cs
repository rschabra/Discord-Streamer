using System.Collections.Concurrent;
using DiscordStreamer.Comp1.Automation;

var builder = WebApplication.CreateBuilder(args);

var comp1Settings = builder.Configuration.GetSection("Comp1").Get<Comp1Settings>() ?? new Comp1Settings();

builder.Services.AddSingleton(comp1Settings);
builder.Services.AddSingleton<TokenStore>();
builder.Services.AddSingleton<ApplianceStateStore>();
builder.Services.AddSingleton<IComp1Automation>(_ => new Comp1AutomationOrchestrator(new Comp1AutomationOptions
{
    PreferredBrowserPath = comp1Settings.PreferredBrowserPath,
    DiscordExecutablePath = comp1Settings.DiscordExecutablePath,
    DiscordScreenShareKeybind = comp1Settings.DiscordScreenShareKeybind,
    DiscordScreenShareKeybindDelayMs = comp1Settings.DiscordScreenShareKeybindDelayMs
}));

var app = builder.Build();

app.MapPost("/api/auth/login", (LoginRequest request, Comp1Settings settings, TokenStore tokenStore) =>
{
    if (request.Username != settings.OperatorUsername || request.Password != settings.OperatorPassword)
    {
        return Results.Unauthorized();
    }

    var session = tokenStore.Create(settings.TokenLifetimeMinutes);
    return Results.Ok(new LoginResponse(session.Token, session.ExpiresAtUtc));
});

app.MapGet("/api/health", (HttpContext httpContext, TokenStore tokenStore, ApplianceStateStore stateStore) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    return Results.Ok(stateStore.ToHealth());
});

app.MapGet("/api/state", (HttpContext httpContext, TokenStore tokenStore, ApplianceStateStore stateStore) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    return Results.Ok(stateStore.ToStateResponse());
});

app.MapPost("/api/browser/open", async (HttpContext httpContext, BrowserOpenApiRequest request, TokenStore tokenStore, ApplianceStateStore stateStore, IComp1Automation automation, CancellationToken cancellationToken) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    var result = await automation.OpenBrowserAsync(new BrowserOpenRequest
    {
        Url = request.Url
    }, cancellationToken);

    stateStore.ApplyAutomationResult(result);

    if (!result.Success)
    {
        stateStore.SetLastError(result.Error);
        return Results.BadRequest(new { error = result.Error, trace = result.TraceEntries });
    }

    stateStore.SetBrowserRunning(true);
    stateStore.SetCurrentUrl(request.Url);
    return Results.Ok(stateStore.ToStateResponse());
});

app.MapPost("/api/apps/discord/launch", async (HttpContext httpContext, TokenStore tokenStore, ApplianceStateStore stateStore, IComp1Automation automation, CancellationToken cancellationToken) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    var result = await automation.LaunchDiscordAsync(cancellationToken);
    stateStore.ApplyAutomationResult(result);

    if (!result.Success)
    {
        stateStore.SetLastError(result.Error);
        return Results.BadRequest(new { error = result.Error, trace = result.TraceEntries });
    }

    stateStore.SetDiscordRunning(true);
    return Results.Ok(stateStore.ToStateResponse());
});

app.MapPost("/api/discord/join-voice", async (HttpContext httpContext, JoinVoiceChannelApiRequest request, TokenStore tokenStore, ApplianceStateStore stateStore, IComp1Automation automation, CancellationToken cancellationToken) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    var result = await automation.JoinVoiceChannelAsync(new JoinVoiceChannelRequest
    {
        ServerName = request.ServerName,
        VoiceChannelName = request.VoiceChannelName
    }, cancellationToken);

    stateStore.ApplyAutomationResult(result);

    if (!result.Success)
    {
        stateStore.SetLastError(result.Error);
        return Results.BadRequest(new { error = result.Error, trace = result.TraceEntries });
    }

    stateStore.SetDiscordRunning(true);
    stateStore.SetSelectedDiscordServer(request.ServerName);
    stateStore.SetSelectedDiscordChannel(request.VoiceChannelName);
    stateStore.ClearLastError();

    return Results.Ok(stateStore.ToStateResponse());
});

app.MapPost("/api/stream/start", async (HttpContext httpContext, StreamStartApiRequest request, TokenStore tokenStore, ApplianceStateStore stateStore, IComp1Automation automation, CancellationToken cancellationToken) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    var result = await automation.StartStreamWorkflowAsync(new StreamStartRequest
    {
        BrowserUrl = request.BrowserUrl,
        DiscordGuildId = request.DiscordGuildId,
        DiscordChannelId = request.DiscordChannelId,
        DiscordServerName = request.DiscordServerName,
        DiscordVoiceChannelName = request.DiscordVoiceChannelName,
        ChannelDisplayName = request.ChannelDisplayName,
        StreamTarget = new StreamTarget
        {
            Kind = request.StreamTarget.Kind,
            WindowTitleHint = request.StreamTarget.WindowTitleHint,
            ProcessNameHint = request.StreamTarget.ProcessNameHint
        },
        IncludeSystemAudio = request.IncludeSystemAudio
    }, cancellationToken);

    stateStore.ApplyAutomationResult(result);

    if (!result.Success)
    {
        stateStore.SetLastError(result.Error);
        return Results.BadRequest(new { error = result.Error, trace = result.TraceEntries });
    }

    if (!string.IsNullOrWhiteSpace(request.DiscordServerName) || !string.IsNullOrWhiteSpace(request.DiscordVoiceChannelName) || !string.IsNullOrWhiteSpace(request.ChannelDisplayName))
    {
        stateStore.SetDiscordRunning(true);
        stateStore.SetSelectedDiscordServer(request.DiscordServerName);
        stateStore.SetSelectedDiscordChannel(request.DiscordVoiceChannelName ?? request.ChannelDisplayName ?? request.DiscordChannelId);
    }

    stateStore.SetStreamActive(true);
    stateStore.SetSelectedStreamWindowTitle(result.SelectedWindowTitle);
    stateStore.ClearLastError();

    return Results.Ok(stateStore.ToStateResponse());
});

app.MapPost("/api/stream/stop", async (HttpContext httpContext, TokenStore tokenStore, ApplianceStateStore stateStore, IComp1Automation automation, CancellationToken cancellationToken) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    var result = await automation.StopStreamAsync(cancellationToken);
    stateStore.ApplyAutomationResult(result);
    stateStore.SetStreamActive(false);

    return Results.Ok(stateStore.ToStateResponse());
});

app.MapPost("/api/developer-mode/launch", (HttpContext httpContext, TokenStore tokenStore, Comp1Settings settings) =>
{
    var authResult = TryAuthorize(httpContext, tokenStore);
    if (authResult is not null)
    {
        return authResult;
    }

    return Results.Ok(new DeveloperModeLaunchResponse(settings.RdpHost, settings.RdpPort, "rdp"));
});

app.Run();

static IResult? TryAuthorize(HttpContext httpContext, TokenStore tokenStore)
{
    var rawHeader = httpContext.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(rawHeader) || !rawHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    var token = rawHeader["Bearer ".Length..].Trim();
    return tokenStore.IsValid(token) ? null : Results.Unauthorized();
}

internal sealed record LoginRequest(string Username, string Password);
internal sealed record LoginResponse(string Token, DateTime ExpiresAtUtc);
internal sealed record DeveloperModeLaunchResponse(string Host, int Port, string Mode);

internal sealed class BrowserOpenApiRequest
{
    public required string Url { get; init; }
}

internal sealed class StreamTargetApiModel
{
    public required string Kind { get; init; }
    public string? WindowTitleHint { get; init; }
    public string? ProcessNameHint { get; init; }
}

internal sealed class StreamStartApiRequest
{
    public string? BrowserUrl { get; init; }
    public string? DiscordGuildId { get; init; }
    public string? DiscordChannelId { get; init; }
    public string? DiscordServerName { get; init; }
    public string? DiscordVoiceChannelName { get; init; }
    public string? ChannelDisplayName { get; init; }
    public required StreamTargetApiModel StreamTarget { get; init; }
    public bool IncludeSystemAudio { get; init; }
}

internal sealed class JoinVoiceChannelApiRequest
{
    public required string ServerName { get; init; }
    public required string VoiceChannelName { get; init; }
}

internal sealed class ApplianceStateStore
{
    private readonly object _gate = new();
    private readonly List<ApiTraceEntry> _traceEntries = [];
    private string? _currentUrl;
    private string? _lastError;
    private string? _selectedDiscordServer;
    private string? _selectedDiscordChannel;
    private string? _selectedStreamWindowTitle;
    private bool _browserRunning;
    private bool _discordRunning;
    private bool _streamActive;

    public ApplianceHealthResponse ToHealth()
    {
        lock (_gate)
        {
            return new ApplianceHealthResponse(
                ComputeStatus(),
                _discordRunning,
                _browserRunning,
                _streamActive);
        }
    }

    public ApplianceStateResponse ToStateResponse()
    {
        lock (_gate)
        {
            return new ApplianceStateResponse(
                ComputeStatus(),
                _discordRunning,
                _browserRunning,
                _streamActive,
                _currentUrl,
                _lastError,
                _selectedDiscordServer,
                _selectedDiscordChannel,
                _selectedStreamWindowTitle,
                _traceEntries.TakeLast(30).ToArray());
        }
    }

    public void ApplyAutomationResult(AutomationResult result)
    {
        lock (_gate)
        {
            foreach (var trace in result.TraceEntries)
            {
                _traceEntries.Add(new ApiTraceEntry(trace.TimestampUtc, trace.Level, trace.Message));
            }

            if (_traceEntries.Count > 100)
            {
                _traceEntries.RemoveRange(0, _traceEntries.Count - 100);
            }
        }
    }

    public void SetCurrentUrl(string? url)
    {
        lock (_gate)
        {
            _currentUrl = url;
        }
    }

    public void SetLastError(string? error)
    {
        lock (_gate)
        {
            _lastError = error;
        }
    }

    public void ClearLastError()
    {
        lock (_gate)
        {
            _lastError = null;
        }
    }

    public void SetSelectedDiscordChannel(string? channel)
    {
        lock (_gate)
        {
            _selectedDiscordChannel = channel;
        }
    }

    public void SetSelectedDiscordServer(string? server)
    {
        lock (_gate)
        {
            _selectedDiscordServer = server;
        }
    }

    public void SetSelectedStreamWindowTitle(string? windowTitle)
    {
        lock (_gate)
        {
            _selectedStreamWindowTitle = windowTitle;
        }
    }

    public void SetBrowserRunning(bool value)
    {
        lock (_gate)
        {
            _browserRunning = value;
        }
    }

    public void SetDiscordRunning(bool value)
    {
        lock (_gate)
        {
            _discordRunning = value;
        }
    }

    public void SetStreamActive(bool value)
    {
        lock (_gate)
        {
            _streamActive = value;
        }
    }

    private string ComputeStatus()
    {
        if (!string.IsNullOrWhiteSpace(_lastError))
        {
            return "degraded";
        }

        return "healthy";
    }
}

internal sealed class TokenStore
{
    private readonly ConcurrentDictionary<string, DateTime> _sessions = new(StringComparer.Ordinal);

    public TokenSession Create(int lifetimeMinutes)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(15, lifetimeMinutes));
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()) + Convert.ToHexString(Guid.NewGuid().ToByteArray());
        _sessions[token] = expiresAtUtc;
        return new TokenSession(token, expiresAtUtc);
    }

    public bool IsValid(string token)
    {
        if (!_sessions.TryGetValue(token, out var expiresAtUtc))
        {
            return false;
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return false;
        }

        return true;
    }
}

internal sealed record TokenSession(string Token, DateTime ExpiresAtUtc);
internal sealed record ApiTraceEntry(DateTime TimestampUtc, string Level, string Message);
internal sealed record ApplianceHealthResponse(string Status, bool DiscordRunning, bool BrowserRunning, bool StreamActive);
internal sealed record ApplianceStateResponse(
    string Status,
    bool DiscordRunning,
    bool BrowserRunning,
    bool StreamActive,
    string? CurrentUrl,
    string? LastError,
    string? SelectedDiscordServer,
    string? SelectedDiscordChannel,
    string? SelectedStreamWindowTitle,
    IReadOnlyList<ApiTraceEntry> LastTraceEntries);

internal sealed class Comp1Settings
{
    public string OperatorUsername { get; init; } = "operator";
    public string OperatorPassword { get; init; } = "replace-with-a-strong-password";
    public string? PreferredBrowserPath { get; init; }
    public string? DiscordExecutablePath { get; init; }
    public string? DiscordScreenShareKeybind { get; init; }
    public int DiscordScreenShareKeybindDelayMs { get; init; } = 2500;
    public string RdpHost { get; init; } = "127.0.0.1";
    public int RdpPort { get; init; } = 3389;
    public int TokenLifetimeMinutes { get; init; } = 480;
}

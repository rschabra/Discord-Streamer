namespace DiscordStreamer.Comp1.Automation;

public sealed class BrowserOpenRequest
{
    public required string Url { get; init; }
}

public sealed class StreamTarget
{
    public required string Kind { get; init; }
    public string? WindowTitleHint { get; init; }
    public string? ProcessNameHint { get; init; }
}

public sealed class StreamStartRequest
{
    public string? BrowserUrl { get; init; }
    public string? DiscordGuildId { get; init; }
    public string? DiscordChannelId { get; init; }
    public string? DiscordServerName { get; init; }
    public string? DiscordVoiceChannelName { get; init; }
    public string? ChannelDisplayName { get; init; }
    public required StreamTarget StreamTarget { get; init; }
    public bool IncludeSystemAudio { get; init; }
}

public sealed class JoinVoiceChannelRequest
{
    public required string ServerName { get; init; }
    public required string VoiceChannelName { get; init; }
}

public sealed class TraceEntry
{
    public required DateTime TimestampUtc { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
}

public sealed class AutomationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? SelectedWindowTitle { get; init; }
    public string? SelectedProcessName { get; init; }
    public List<TraceEntry> TraceEntries { get; init; } = [];
}

public sealed class Comp1AutomationOptions
{
    public string? PreferredBrowserPath { get; init; }
    public string? DiscordExecutablePath { get; init; }
    public string? DiscordScreenShareKeybind { get; init; }
}

using System.Diagnostics;

namespace DiscordStreamer.Comp1.Automation;

public sealed class Comp1AutomationOrchestrator(Comp1AutomationOptions options) : IComp1Automation
{
    public Task<AutomationResult> LaunchDiscordAsync(CancellationToken cancellationToken)
    {
        var trace = new List<TraceEntry>
        {
            Info("Starting Discord launch request.")
        };

        try
        {
            var executable = string.IsNullOrWhiteSpace(options.DiscordExecutablePath)
                ? "discord"
                : options.DiscordExecutablePath;

            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true
            });

            trace.Add(Info($"Discord launch attempted using '{executable}'."));
            trace.Add(Info("Real UI/session verification must be validated on target hardware."));

            return Task.FromResult(new AutomationResult
            {
                Success = true,
                TraceEntries = trace
            });
        }
        catch (Exception ex)
        {
            trace.Add(Error($"Discord launch failed: {ex.Message}"));
            return Task.FromResult(new AutomationResult
            {
                Success = false,
                Error = ex.Message,
                TraceEntries = trace
            });
        }
    }

    public Task<AutomationResult> OpenBrowserAsync(BrowserOpenRequest request, CancellationToken cancellationToken)
    {
        var trace = new List<TraceEntry>
        {
            Info($"Opening browser URL '{request.Url}'.")
        };

        try
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            {
                trace.Add(Error("Browser URL was invalid."));
                return Task.FromResult(new AutomationResult
                {
                    Success = false,
                    Error = "Invalid URL.",
                    TraceEntries = trace
                });
            }

            if (string.IsNullOrWhiteSpace(options.PreferredBrowserPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = request.Url,
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = options.PreferredBrowserPath,
                    Arguments = request.Url,
                    UseShellExecute = true
                });
            }

            trace.Add(Info("Browser launch attempted."));

            return Task.FromResult(new AutomationResult
            {
                Success = true,
                TraceEntries = trace
            });
        }
        catch (Exception ex)
        {
            trace.Add(Error($"Browser launch failed: {ex.Message}"));
            return Task.FromResult(new AutomationResult
            {
                Success = false,
                Error = ex.Message,
                TraceEntries = trace
            });
        }
    }

    public async Task<AutomationResult> StartStreamWorkflowAsync(StreamStartRequest request, CancellationToken cancellationToken)
    {
        var trace = new List<TraceEntry>
        {
            Info("Starting stream workflow."),
            Info($"Stream target kind: {request.StreamTarget.Kind}."),
            Info($"Include system audio: {request.IncludeSystemAudio}.")
        };

        if (!string.IsNullOrWhiteSpace(request.BrowserUrl))
        {
            var browserResult = await OpenBrowserAsync(new BrowserOpenRequest
            {
                Url = request.BrowserUrl
            }, cancellationToken);

            trace.AddRange(browserResult.TraceEntries);
            if (!browserResult.Success)
            {
                return new AutomationResult
                {
                    Success = false,
                    Error = browserResult.Error,
                    TraceEntries = trace
                };
            }
        }

        var discordResult = await LaunchDiscordAsync(cancellationToken);
        trace.AddRange(discordResult.TraceEntries);
        if (!discordResult.Success)
        {
            return new AutomationResult
            {
                Success = false,
                Error = discordResult.Error,
                TraceEntries = trace
            };
        }

        trace.Add(Info("Join-call and begin-stream steps are currently scaffolded for selector tuning."));
        trace.Add(Info($"Requested Discord destination: {request.ChannelDisplayName ?? request.DiscordChannelId ?? "unspecified"}."));
        trace.Add(Info($"Requested window hint: {request.StreamTarget.WindowTitleHint ?? "none"}."));

        await Task.Delay(100, cancellationToken);

        return new AutomationResult
        {
            Success = true,
            TraceEntries = trace
        };
    }

    public Task<AutomationResult> StopStreamAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new AutomationResult
        {
            Success = true,
            TraceEntries =
            [
                Info("Stop-stream signal recorded. Discord teardown remains part of the automation spike.")
            ]
        });
    }

    private static TraceEntry Info(string message) => new()
    {
        TimestampUtc = DateTime.UtcNow,
        Level = "info",
        Message = message
    };

    private static TraceEntry Error(string message) => new()
    {
        TimestampUtc = DateTime.UtcNow,
        Level = "error",
        Message = message
    };
}

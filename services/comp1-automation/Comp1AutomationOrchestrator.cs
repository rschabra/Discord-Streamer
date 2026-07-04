using System.Diagnostics;
using System.Text;

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

    public async Task<AutomationResult> JoinVoiceChannelAsync(JoinVoiceChannelRequest request, CancellationToken cancellationToken)
    {
        var trace = new List<TraceEntry>
        {
            Info($"Starting join-voice request for server '{request.ServerName}' and channel '{request.VoiceChannelName}'.")
        };

        if (string.IsNullOrWhiteSpace(request.ServerName) || string.IsNullOrWhiteSpace(request.VoiceChannelName))
        {
            trace.Add(Error("Server name and voice channel name are both required."));
            return new AutomationResult
            {
                Success = false,
                Error = "Server name and voice channel name are required.",
                TraceEntries = trace
            };
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

        try
        {
            var searchQuery = $"{request.ServerName} #{request.VoiceChannelName}";
            trace.Add(Info($"Using Discord quick switch search query '{searchQuery}'."));

            var discordProcess = await WaitForDiscordWindowAsync(cancellationToken);
            trace.Add(Info($"Using Discord process {discordProcess.Id} with window '{discordProcess.MainWindowTitle}'."));

            await RunDiscordQuickSwitchJoinAsync(searchQuery, discordProcess.Id, cancellationToken);

            trace.Add(Info("Discord quick switch join attempt sent."));
            trace.Add(Info("Verify that the target voice channel was unique and visible in Discord search results."));

            return new AutomationResult
            {
                Success = true,
                TraceEntries = trace
            };
        }
        catch (Exception ex)
        {
            trace.Add(Error($"Join-voice automation failed: {ex.Message}"));
            return new AutomationResult
            {
                Success = false,
                Error = ex.Message,
                TraceEntries = trace
            };
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

        if (!string.IsNullOrWhiteSpace(request.DiscordServerName) || !string.IsNullOrWhiteSpace(request.DiscordVoiceChannelName))
        {
            var joinResult = await JoinVoiceChannelAsync(new JoinVoiceChannelRequest
            {
                ServerName = request.DiscordServerName ?? "unspecified",
                VoiceChannelName = request.DiscordVoiceChannelName ?? request.ChannelDisplayName ?? "unspecified"
            }, cancellationToken);

            trace.AddRange(joinResult.TraceEntries);
            if (!joinResult.Success)
            {
                return new AutomationResult
                {
                    Success = false,
                    Error = joinResult.Error,
                    TraceEntries = trace
                };
            }
        }
        else
        {
            trace.Add(Info("Join-call step was skipped because no server/channel names were provided."));
        }

        trace.Add(Info("Begin-stream steps are still scaffolded for selector tuning."));
        trace.Add(Info($"Requested Discord destination: {request.DiscordServerName ?? "unspecified"} / {request.DiscordVoiceChannelName ?? request.ChannelDisplayName ?? request.DiscordChannelId ?? "unspecified"}."));
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

    private static async Task<Process> WaitForDiscordWindowAsync(CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(15);

        while (DateTime.UtcNow < timeoutAt)
        {
            var discordProcess = Process.GetProcessesByName("Discord")
                .OrderByDescending(GetSafeStartTime)
                .FirstOrDefault(process =>
                {
                    try
                    {
                        process.Refresh();
                        return process.MainWindowHandle != IntPtr.Zero;
                    }
                    catch
                    {
                        return false;
                    }
                });

            if (discordProcess is not null)
            {
                discordProcess.Refresh();
                return discordProcess;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new InvalidOperationException("Discord process started, but no focusable main window appeared within 15 seconds.");
    }

    private static DateTime GetSafeStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static async Task RunDiscordQuickSwitchJoinAsync(string searchQuery, int discordProcessId, CancellationToken cancellationToken)
    {
        var escapedQuery = EscapeSendKeysText(searchQuery);
        var script = $$"""
            $wshell = New-Object -ComObject WScript.Shell
            if (-not $wshell.AppActivate({{discordProcessId}})) { throw 'Discord process window not found or not focusable.' }
            Start-Sleep -Milliseconds 1200
            $wshell.SendKeys('^k')
            Start-Sleep -Milliseconds 600
            $wshell.SendKeys('^a')
            Start-Sleep -Milliseconds 150
            $wshell.SendKeys('{BACKSPACE}')
            Start-Sleep -Milliseconds 300
            $wshell.SendKeys('{{escapedQuery}}')
            Start-Sleep -Milliseconds 900
            $wshell.SendKeys('{ENTER}')
            """;

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        }) ?? throw new InvalidOperationException("Failed to start PowerShell automation process.");

        await process.WaitForExitAsync(cancellationToken);

        var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stdErr)
                ? $"Quick switch automation failed with exit code {process.ExitCode}. {stdOut}".Trim()
                : stdErr.Trim());
        }
    }

    private static string EscapeSendKeysText(string text)
    {
        var builder = new StringBuilder();

        foreach (var character in text)
        {
            builder.Append(character switch
            {
                '+' => "{+}",
                '^' => "{^}",
                '%' => "{%}",
                '~' => "{~}",
                '(' => "{(}",
                ')' => "{)}",
                '[' => "{[}",
                ']' => "{]}",
                '{' => "{{}",
                '}' => "{}}",
                _ => character.ToString()
            });
        }

        return builder.ToString();
    }
}

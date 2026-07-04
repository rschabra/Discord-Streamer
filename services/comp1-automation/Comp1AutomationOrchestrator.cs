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
            var searchQuery = BuildDiscordQuickSwitchQuery(request.ServerName, request.VoiceChannelName);
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

        WindowCandidate? matchedTargetWindow = null;

        try
        {
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

            if (request.StreamTarget.Kind is "browser" or "application")
            {
                var resolution = await ResolveTargetWindowAsync(request.StreamTarget, cancellationToken);
                matchedTargetWindow = resolution.SelectedWindow;
                trace.Add(Info($"Resolved stream target window '{matchedTargetWindow.MainWindowTitle}' from process '{matchedTargetWindow.ProcessName}' (PID {matchedTargetWindow.ProcessId})."));

                if (resolution.CandidateCount > 1)
                {
                    trace.Add(Info($"Multiple candidate windows matched the hint; selected the highest-confidence match out of {resolution.CandidateCount} candidates."));
                }
            }
            else
            {
                trace.Add(Info("Display target selected; window hint resolution was skipped."));
            }

            if (matchedTargetWindow is null)
            {
                return new AutomationResult
                {
                    Success = false,
                    Error = "Display streaming is not implemented yet; provide an application or browser window hint.",
                    TraceEntries = trace
                };
            }

            var activeDiscordProcess = await WaitForDiscordWindowAsync(cancellationToken);
            trace.Add(Info($"Preparing Discord share dialog with process {activeDiscordProcess.Id}."));

            var shareTraceLines = await RunDiscordShareDialogStartAsync(
                matchedTargetWindow,
                request.StreamTarget.WindowTitleHint,
                request.IncludeSystemAudio,
                activeDiscordProcess.Id,
                cancellationToken);

            foreach (var line in shareTraceLines)
            {
                trace.Add(Info(line));
            }

            trace.Add(Info("Discord share-dialog automation attempted to start the stream."));
            trace.Add(Info($"Requested Discord destination: {request.DiscordServerName ?? "unspecified"} / {request.DiscordVoiceChannelName ?? request.ChannelDisplayName ?? request.DiscordChannelId ?? "unspecified"}."));
            trace.Add(Info($"Requested window hint: {request.StreamTarget.WindowTitleHint ?? "none"}."));

            await Task.Delay(100, cancellationToken);

            return new AutomationResult
            {
                Success = true,
                SelectedWindowTitle = matchedTargetWindow?.MainWindowTitle,
                SelectedProcessName = matchedTargetWindow?.ProcessName,
                TraceEntries = trace
            };
        }
        catch (Exception ex)
        {
            trace.Add(Error($"Stream automation failed: {ex.Message}"));
            return new AutomationResult
            {
                Success = false,
                Error = ex.Message,
                SelectedWindowTitle = matchedTargetWindow?.MainWindowTitle,
                SelectedProcessName = matchedTargetWindow?.ProcessName,
                TraceEntries = trace
            };
        }
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

    private static async Task<IReadOnlyList<string>> RunDiscordShareDialogStartAsync(
        WindowCandidate matchedTargetWindow,
        string? windowTitleHint,
        bool includeSystemAudio,
        int discordProcessId,
        CancellationToken cancellationToken)
    {
        var searchFragments = BuildWindowSearchFragments(matchedTargetWindow.MainWindowTitle, windowTitleHint);
        var searchFragmentsLiteral = ToPowerShellArrayLiteral(searchFragments);
        var includeSystemAudioLiteral = includeSystemAudio ? "$true" : "$false";

        var script = $$"""
            Add-Type -AssemblyName UIAutomationClient
            Add-Type -AssemblyName UIAutomationTypes
            Add-Type @"
            using System;
            using System.Runtime.InteropServices;
            public static class CursorAutomation {
                [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
                [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
                public const uint LeftDown = 0x0002;
                public const uint LeftUp = 0x0004;
            }
            "@

            function Get-DiscordRoot($processId) {
                $process = Get-Process -Id $processId -ErrorAction Stop
                $process.Refresh()
                if ($process.MainWindowHandle -eq 0) { throw "Discord main window handle is not available." }
                return [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
            }

            function Get-VisibleDescendants($root) {
                $elements = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
                foreach ($element in $elements) {
                    try {
                        $rect = $element.Current.BoundingRectangle
                        if ($rect.Width -gt 0 -and $rect.Height -gt 0) { $element }
                    } catch {}
                }
            }

            function Matches-ControlType($element, [string[]]$controlTypes) {
                if (-not $controlTypes -or $controlTypes.Count -eq 0) { return $true }
                try {
                    $typeName = $element.Current.ControlType.ProgrammaticName.ToLowerInvariant()
                    foreach ($controlType in $controlTypes) {
                        if ($typeName.Contains($controlType.ToLowerInvariant())) { return $true }
                    }
                } catch {}
                return $false
            }

            function Find-ElementByPreferredNames($root, [string[]]$exactNames, [string[]]$containsNames, [string[]]$controlTypes, [string[]]$excludeFragments) {
                $matches = @()
                foreach ($element in Get-VisibleDescendants $root) {
                    try { $name = $element.Current.Name } catch { continue }
                    if ([string]::IsNullOrWhiteSpace($name)) { continue }
                    if (-not (Matches-ControlType $element $controlTypes)) { continue }

                    $excluded = $false
                    foreach ($excludeFragment in $excludeFragments) {
                        if (-not [string]::IsNullOrWhiteSpace($excludeFragment) -and $name.IndexOf($excludeFragment, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                            $excluded = $true
                            break
                        }
                    }
                    if ($excluded) { continue }

                    foreach ($exactName in $exactNames) {
                        if ($name.Equals($exactName, [StringComparison]::OrdinalIgnoreCase)) {
                            return $element
                        }
                    }

                    foreach ($fragment in $containsNames) {
                        if (-not [string]::IsNullOrWhiteSpace($fragment) -and $name.IndexOf($fragment, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                            $matches += [pscustomobject]@{ Element = $element; Name = $name; Length = $name.Length; Fragment = $fragment }
                            break
                        }
                    }
                }

                return $matches | Sort-Object Length | Select-Object -First 1 -ExpandProperty Element
            }

            function Wait-ForElementByPreferredNames($processId, [string[]]$exactNames, [string[]]$containsNames, [string[]]$controlTypes, [string[]]$excludeFragments, [int]$timeoutMs) {
                $start = Get-Date
                while (((Get-Date) - $start).TotalMilliseconds -lt $timeoutMs) {
                    $root = Get-DiscordRoot $processId
                    $element = Find-ElementByPreferredNames $root $exactNames $containsNames $controlTypes $excludeFragments
                    if ($element) { return $element }
                    Start-Sleep -Milliseconds 300
                }
                $root = Get-DiscordRoot $processId
                $visibleNames = Get-VisibleDescendants $root |
                    ForEach-Object {
                        try {
                            $name = $_.Current.Name
                            $typeName = $_.Current.ControlType.ProgrammaticName
                            if (-not [string]::IsNullOrWhiteSpace($name)) {
                                "$name [$typeName]"
                            }
                        } catch {}
                    } |
                    Select-Object -Unique |
                    Select-Object -First 40

                $visibleSummary = if ($visibleNames) { $visibleNames -join '; ' } else { 'No visible named controls found.' }
                throw "Timed out waiting for a Discord UI element. Names: $($exactNames -join ', ') / $($containsNames -join ', '). Visible named controls: $visibleSummary"
            }

            function Invoke-AutomationElement($element, $description) {
                if ($null -eq $element) { throw "Element not found: $description" }

                try {
                    $invoke = $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                    if ($invoke) {
                        $invoke.Invoke()
                        Write-Output "Invoked $description"
                        return
                    }
                } catch {}

                try {
                    $selection = $element.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
                    if ($selection) {
                        $selection.Select()
                        Write-Output "Selected $description"
                        return
                    }
                } catch {}

                try {
                    $toggle = $element.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
                    if ($toggle) {
                        $toggle.Toggle()
                        Write-Output "Toggled $description"
                        return
                    }
                } catch {}

                $rect = $element.Current.BoundingRectangle
                if ($rect.Width -le 0 -or $rect.Height -le 0) { throw "No actionable bounds for $description" }

                $x = [int]($rect.X + ($rect.Width / 2))
                $y = [int]($rect.Y + ($rect.Height / 2))
                [CursorAutomation]::SetCursorPos($x, $y) | Out-Null
                Start-Sleep -Milliseconds 100
                [CursorAutomation]::mouse_event([CursorAutomation]::LeftDown, 0, 0, 0, [UIntPtr]::Zero)
                Start-Sleep -Milliseconds 50
                [CursorAutomation]::mouse_event([CursorAutomation]::LeftUp, 0, 0, 0, [UIntPtr]::Zero)
                Write-Output "Clicked $description at ($x,$y)"
            }

            $discordProcessId = {{discordProcessId}}
            $targetFragments = {{searchFragmentsLiteral}}
            $includeSystemAudio = {{includeSystemAudioLiteral}}

            $wshell = New-Object -ComObject WScript.Shell
            if (-not $wshell.AppActivate($discordProcessId)) { throw "Discord process window not found or not focusable." }
            Start-Sleep -Milliseconds 1200
            Write-Output "Discord window activated."

            $shareButton = Wait-ForElementByPreferredNames $discordProcessId @('Share Your Screen') @('Share Your Screen', 'Screen') @('button') @() 10000
            Invoke-AutomationElement $shareButton 'share button'
            Start-Sleep -Milliseconds 1200

            $applicationsTab = Find-ElementByPreferredNames (Get-DiscordRoot $discordProcessId) @('Applications') @('Applications') @('tabitem', 'button') @()
            if ($applicationsTab) {
                Invoke-AutomationElement $applicationsTab 'applications tab'
                Start-Sleep -Milliseconds 700
            } else {
                Write-Output "Applications tab not found; continuing with the current share view."
            }

            $targetElement = Wait-ForElementByPreferredNames $discordProcessId @() $targetFragments @('listitem', 'button', 'pane', 'group', 'text', 'image') @('Share Your Screen', 'Applications', 'Go Live') 10000
            Invoke-AutomationElement $targetElement 'target application tile'
            Start-Sleep -Milliseconds 700

            if ($includeSystemAudio) {
                $audioElement = Find-ElementByPreferredNames (Get-DiscordRoot $discordProcessId) @() @('Share Audio', 'Also share application audio', 'Sound') @('checkbox', 'button') @()
                if ($audioElement) {
                    try {
                        $togglePattern = $audioElement.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
                        if ($togglePattern.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::Off) {
                            $togglePattern.Toggle()
                            Write-Output "Enabled application audio."
                        } else {
                            Write-Output "Application audio already enabled."
                        }
                    } catch {
                        Write-Output "Audio toggle was found but did not expose TogglePattern; leaving it unchanged."
                    }
                } else {
                    Write-Output "Application audio toggle was not found."
                }
                Start-Sleep -Milliseconds 300
            }

            $goLiveButton = Wait-ForElementByPreferredNames $discordProcessId @('Go Live') @('Go Live', 'Share') @('button') @('Share Your Screen') 10000
            Invoke-AutomationElement $goLiveButton 'go live button'
            Start-Sleep -Milliseconds 1200
            Write-Output "Go Live command sent."
            """;

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        }) ?? throw new InvalidOperationException("Failed to start Discord share automation process.");

        await process.WaitForExitAsync(cancellationToken);

        var stdOut = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErr = await process.StandardError.ReadToEndAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stdErr)
                ? $"Share dialog automation failed with exit code {process.ExitCode}. {stdOut}".Trim()
                : stdErr.Trim());
        }

        return stdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static async Task<WindowResolutionResult> ResolveTargetWindowAsync(StreamTarget target, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target.WindowTitleHint) && string.IsNullOrWhiteSpace(target.ProcessNameHint))
        {
            throw new InvalidOperationException("A window title hint or process name hint is required to resolve the stream target window.");
        }

        var timeoutAt = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < timeoutAt)
        {
            var candidates = FindWindowCandidates(target).ToArray();
            if (candidates.Length > 0)
            {
                return new WindowResolutionResult(candidates[0], candidates.Length);
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new InvalidOperationException($"No open window matched the requested hint '{target.WindowTitleHint ?? target.ProcessNameHint}'.");
    }

    private static IEnumerable<WindowCandidate> FindWindowCandidates(StreamTarget target)
    {
        var titleHint = target.WindowTitleHint?.Trim();
        var processHint = target.ProcessNameHint?.Trim();

        return Process.GetProcesses()
            .Select(process =>
            {
                try
                {
                    process.Refresh();
                    if (process.MainWindowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    {
                        return null;
                    }

                    return new WindowCandidate(
                        process.Id,
                        process.ProcessName,
                        process.MainWindowTitle,
                        ScoreWindowCandidate(process, process.MainWindowTitle, titleHint, processHint));
                }
                catch
                {
                    return null;
                }
            })
            .Where(candidate => candidate is not null && candidate.Score > 0)
            .OrderByDescending(candidate => candidate!.Score)
            .ThenBy(candidate => candidate!.MainWindowTitle.Length)
            .Cast<WindowCandidate>();
    }

    private static int ScoreWindowCandidate(Process process, string windowTitle, string? titleHint, string? processHint)
    {
        var score = 0;

        if (!string.IsNullOrWhiteSpace(titleHint))
        {
            if (string.Equals(windowTitle, titleHint, StringComparison.OrdinalIgnoreCase))
            {
                score += 100;
            }
            else if (windowTitle.Contains(titleHint, StringComparison.OrdinalIgnoreCase))
            {
                score += 70;
            }
        }

        if (!string.IsNullOrWhiteSpace(processHint))
        {
            if (string.Equals(process.ProcessName, processHint, StringComparison.OrdinalIgnoreCase))
            {
                score += 60;
            }
            else if (process.ProcessName.Contains(processHint, StringComparison.OrdinalIgnoreCase))
            {
                score += 30;
            }
        }

        return score;
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

    private static string BuildDiscordQuickSwitchQuery(string serverName, string voiceChannelName)
    {
        var compactChannelName = new string(voiceChannelName.Where(character => !char.IsWhiteSpace(character)).ToArray());
        return $"{serverName} !{compactChannelName}";
    }

    private static string[] BuildWindowSearchFragments(string resolvedWindowTitle, string? windowTitleHint)
    {
        var fragments = new List<string>();

        AddFragment(windowTitleHint);
        AddFragment(resolvedWindowTitle);

        foreach (var separator in new[] { " - ", " | ", " — ", " – ", ":" })
        {
            var index = resolvedWindowTitle.IndexOf(separator, StringComparison.Ordinal);
            if (index > 3)
            {
                AddFragment(resolvedWindowTitle[..index]);
            }
        }

        return fragments
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(fragment => fragment.Length)
            .ToArray();

        void AddFragment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim();
            if (normalized.Length >= 3)
            {
                fragments.Add(normalized);
            }
        }
    }

    private static string ToPowerShellArrayLiteral(IEnumerable<string> values)
    {
        var items = values
            .Select(value => $"'{value.Replace("'", "''")}'")
            .ToArray();

        return $"@({string.Join(", ", items)})";
    }

    private sealed record WindowCandidate(int ProcessId, string ProcessName, string MainWindowTitle, int Score);
    private sealed record WindowResolutionResult(WindowCandidate SelectedWindow, int CandidateCount);
}

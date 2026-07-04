namespace DiscordStreamer.Comp1.Automation;

public interface IComp1Automation
{
    Task<AutomationResult> LaunchDiscordAsync(CancellationToken cancellationToken);
    Task<AutomationResult> OpenBrowserAsync(BrowserOpenRequest request, CancellationToken cancellationToken);
    Task<AutomationResult> StartStreamWorkflowAsync(StreamStartRequest request, CancellationToken cancellationToken);
    Task<AutomationResult> StopStreamAsync(CancellationToken cancellationToken);
}

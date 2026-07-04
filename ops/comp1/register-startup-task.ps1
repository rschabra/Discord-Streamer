param(
    [string]$PublishedAgentPath = "C:\Discord-Streamer\out\comp1-agent\DiscordStreamer.Comp1.Agent.exe",
    [string]$TaskName = "DiscordStreamerComp1Agent"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $PublishedAgentPath)) {
    throw "Published agent not found at $PublishedAgentPath"
}

$action = New-ScheduledTaskAction -Execute $PublishedAgentPath
$trigger = New-ScheduledTaskTrigger -AtStartup
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -StartWhenAvailable

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Description "Starts the Discord Streamer COMP1 agent at boot" -Force

Write-Host "Scheduled task '$TaskName' registered."

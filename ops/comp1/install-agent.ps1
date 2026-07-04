param(
    [string]$ProjectRoot = "C:\Discord-Streamer",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Preparing COMP1 agent install from $ProjectRoot"

$agentPath = Join-Path $ProjectRoot "services\comp1-agent"

if (-not (Test-Path $agentPath)) {
    throw "Agent path not found: $agentPath"
}

Write-Host "Build and publish the agent manually or through CI once .NET SDK is installed."
Write-Host "Recommended next command:"
Write-Host "dotnet publish `"$agentPath`" -c $Configuration -o `"$ProjectRoot\out\comp1-agent`""
Write-Host ""
Write-Host "After publishing, register the agent for startup using either:"
Write-Host "- a Windows Service wrapper"
Write-Host "- a Scheduled Task that runs at boot"
Write-Host ""
Write-Host "This script is intentionally conservative and documents the install flow without making system-wide changes yet."

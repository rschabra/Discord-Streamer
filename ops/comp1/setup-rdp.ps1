$ErrorActionPreference = "Stop"

Write-Host "Enabling Remote Desktop on COMP1"

Set-ItemProperty -Path "HKLM:\System\CurrentControlSet\Control\Terminal Server" -Name "fDenyTSConnections" -Value 0
Enable-NetFirewallRule -DisplayGroup "Remote Desktop"

Write-Host "Remote Desktop has been enabled."
Write-Host "Confirm that COMP1 is running Windows 11 Pro and that the chosen operator account is allowed to log in through RDP."

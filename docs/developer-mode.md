# Developer Mode

## Recommendation

Use Windows RDP as the default developer-mode transport on `COMP1`.

Reasons:

- it is native to Windows 11 Pro
- it keeps remote-desktop concerns separate from the control API
- it is good enough for debugging launch failures, Discord state drift, and browser playback issues
- `COMP2` can launch it with a normal `mstsc` process

## Flow

1. `COMP2` calls `POST /api/developer-mode/launch`.
2. `COMP1` returns the RDP host and port.
3. The desktop app launches `mstsc /v:<host>:<port>`.
4. Operator takes over the desktop directly.

## Fallback

If native RDP proves awkward for unattended recovery or app-session visibility, evaluate a fallback remote-desktop product. That fallback should still remain outside the main appliance API.

## Security Notes

- keep RDP limited to the local network unless a VPN is introduced later
- use a strong Windows account password on `COMP1`
- avoid routing arbitrary keyboard/mouse events through the HTTP API
- document whether the machine uses auto-login, because it affects what RDP can attach to after reboot

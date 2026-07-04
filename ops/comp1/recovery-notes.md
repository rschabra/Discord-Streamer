# Recovery Notes

## Boot Behavior

The target appliance behavior is:

- machine powers on after power loss
- operator account or agent startup path returns the box to a controllable state
- `COMP1` agent is reachable from `COMP2`
- Discord and browser can be relaunched on demand

## Recommended Windows Settings

- enable BIOS or UEFI power restore after outage
- keep Ethernet as the preferred network path
- disable sleep states that interfere with unattended access
- decide explicitly whether to use auto-login or a stricter sign-in flow

## Failure Modes To Watch

- Discord updates that change selector layout
- browser updates that alter window titles or autoplay behavior
- RDP session behavior changing visible app state
- local firewall rules blocking the control API

## Manual Recovery Sequence

1. Open developer mode from the `COMP2` app.
2. Verify the Windows session is active.
3. Check whether Discord is logged in.
4. Confirm the browser can open the requested URL.
5. Retry the stream workflow and inspect trace entries.

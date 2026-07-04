# Hardware Recommendation

## Purchase Target

The recommended `COMP1` target is a Windows 11 Pro mini PC in this class:

- CPU: `AMD Ryzen 7 8845HS` class or newer equivalent
- Memory: `32 GB RAM`
- Storage: `1 TB NVMe SSD`
- Network: `1 GbE` minimum, `2.5 GbE` preferred
- Graphics: integrated graphics are sufficient for browser playback, Discord, RDP, and system-audio streaming
- OS: `Windows 11 Pro`

This is the best balanced-v1 target because it gives comfortable headroom for:

- Discord desktop client
- browser video playback
- remote desktop sessions
- background automation and logging
- future LAN device bridge work

## Why This Tier

Lower-end mini PCs can work for basic proof-of-concepts, but a Ryzen 7 class box with `32 GB` RAM and a `1 TB` SSD reduces avoidable risk in four important areas:

- smoother browser media playback during Discord streaming
- better multitasking under RDP plus Discord plus automation
- more comfortable storage for logs, assets, and future experiments
- less chance that hardware bottlenecks are confused with automation bugs

## Acceptable Minimum

If you need to trim cost, this is the lowest tier worth targeting:

- CPU: recent Intel N100/N305 or equivalent
- Memory: `16 GB RAM`
- Storage: `512 GB NVMe`
- OS: `Windows 11 Pro`
- Network: wired Ethernet

That tier is acceptable for a prototype, but it is not the preferred balanced-v1 target.

## Avoid For V1

- fanless ultra-low-power boxes with weak sustained performance
- `8 GB` RAM systems
- storage smaller than `512 GB`
- Windows Home if you want native RDP available
- Wi-Fi-only setups when Ethernet is available

## Buying Checklist

Use this checklist when selecting a concrete listing:

- includes or supports `Windows 11 Pro`
- user-upgradeable RAM or already ships with `32 GB`
- ships with `1 TB NVMe` or has an easy expansion path
- has at least one reliable Ethernet port
- has enough USB ports for occasional local setup
- supports BIOS power-on after power loss
- does not depend on a discrete GPU for the target workload

## Final Recommendation

Buy a `Ryzen 7 8845HS` class mini PC with `32 GB RAM`, `1 TB NVMe`, `Windows 11 Pro`, and wired Ethernet support. If pricing or availability forces a step down, use a `16 GB / 512 GB` Intel N100-class machine only for the prototype stage.

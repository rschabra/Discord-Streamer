# Discord Streaming Appliance

`Discord-Streamer` is a Windows-first LAN appliance project for controlling a dedicated streaming machine (`COMP1`) from a desktop controller on another machine (`COMP2`).

The initial goal is a balanced v1 that can:

- authenticate from `COMP2` to `COMP1` on the local network
- launch Discord on `COMP1`
- open a browser URL on `COMP1`
- join a selected Discord call or workflow
- start streaming browser media with system audio
- fall back to developer-mode remote takeover when automation gets stuck
- recover cleanly after reboot or power loss

## Repository Layout

- `apps/comp2-desktop/`: Electron-based TypeScript controller for `COMP2`
- `services/comp1-agent/`: .NET control API and appliance runtime for `COMP1`
- `services/comp1-automation/`: automation primitives and orchestration for Discord/browser control
- `packages/shared-contracts/`: shared TypeScript contracts used by the desktop app
- `ops/comp1/`: Windows setup, recovery, and RDP helper scripts
- `docs/`: architecture, hardware, API, developer mode, backlog, and spike notes

## Current Status

This repo now contains the first implementation scaffold for the plan:

- monorepo layout and bootstrap docs
- a concrete hardware target spec for `COMP1`
- a documented LAN API contract
- a minimal .NET agent with authenticated control endpoints
- an Electron shell for the `COMP2` desktop controller
- an initial automation orchestration layer for Discord/browser startup
- developer-mode RDP launch hooks
- a Trello-friendly backlog and milestone breakdown

## Recommended Runtime Stack

- `COMP1`: Windows 11 Pro mini PC or small-form-factor desktop
- `COMP2`: Windows desktop running the Electron controller
- `COMP1` backend: .NET 8 minimal API
- `COMP2` app: TypeScript + Electron

## Quick Start

The codebase is scaffolded for a Windows machine with `Node.js` and `.NET 8` installed.

1. Install `Node.js` and `.NET 8 SDK`.
2. Fill in `services/comp1-agent/appsettings.json` with a secure username/password and local appliance settings.
3. Build and run the `COMP1` agent.
4. Build and run the `COMP2` desktop app.
5. Use the desktop app to log into `COMP1`, send a browser URL, and trigger the stream workflow.

## Important Caveats

The riskiest part of the project remains Discord UI automation. This scaffold isolates that risk in `services/comp1-automation/` and documents the spike plan in `docs/automation-spike.md`, but real selector tuning and runtime verification will still be required on the target hardware.

# Trello Backlog

## Lists

- `Ideas`
- `Architecture / Research`
- `Ready`
- `In Progress`
- `Blocked`
- `Testing`
- `Done`

## Epic: Hardware And OS Selection

### Card: Confirm `COMP1` hardware purchase target

- choose the Windows mini PC tier
- confirm `32 GB RAM`, `1 TB NVMe`, and `Windows 11 Pro`
- confirm Ethernet-first setup

Acceptance criteria:

- target spec recorded in `docs/hardware.md`
- buying checklist documented

## Epic: Repo Bootstrap And Docs

### Card: Create monorepo scaffold

- add root `README.md`
- add docs for architecture, API, hardware, developer mode, and automation spike
- create `apps`, `services`, `packages`, and `ops` structure

Acceptance criteria:

- repository layout is visible and documented
- docs reflect the v1 architecture

## Epic: Shared Contracts

### Card: Define desktop-side request and state types

- login request/response
- health/state response
- browser open request
- stream start request
- developer-mode launch response

Acceptance criteria:

- exported from `packages/shared-contracts/src/index.ts`

## Epic: `COMP1` API And Auth

### Card: Implement authenticated LAN API

- username/password login endpoint
- bearer token validation
- state and health endpoints
- browser, Discord, stream, and developer-mode endpoints

Acceptance criteria:

- minimal API starts with configured credentials
- protected routes reject missing or invalid tokens

## Epic: `COMP2` Desktop Shell

### Card: Build operator console shell

- login form
- state and health panel
- URL and Discord target inputs
- action buttons for launch, stream, stop, and developer mode

Acceptance criteria:

- renderer can authenticate and invoke the control endpoints through preload IPC

## Epic: Discord Launch Automation

### Card: Start or focus Discord

- detect Discord executable path
- start Discord when missing
- record trace entries

Acceptance criteria:

- agent reports whether Discord launch succeeded

## Epic: Browser URL Playback Automation

### Card: Open browser URL from `COMP2`

- validate URL
- launch preferred browser
- track current URL

Acceptance criteria:

- state reflects the opened URL and browser status

## Epic: Discord Call Join And Stream Automation

### Card: Orchestrate end-to-end stream workflow

- optionally open URL
- launch Discord
- join selected destination
- start stream with system audio

Acceptance criteria:

- trace buffer shows every workflow step
- failures surface clearly to `COMP2`

## Epic: Developer-Mode Remote Desktop

### Card: Launch RDP from controller app

- request launch metadata from `COMP1`
- start `mstsc` on `COMP2`

Acceptance criteria:

- operator can open RDP directly from the app

## Epic: Boot / Recovery / Watchdog

### Card: Define startup and recovery scripts

- document service startup
- script RDP enablement
- record recovery expectations after reboot

Acceptance criteria:

- `ops/comp1/` contains setup artifacts and recovery notes

## Epic: HTTP LAN Device Integration

### Card: Reserve plugin-friendly device action shape

- document future device action contract
- keep the route family separate from Discord automation

Acceptance criteria:

- architecture and API docs identify the future device bridge boundary

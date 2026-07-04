# Discord And Browser Automation Spike

## Goal

Prove the highest-risk vertical slice on real `COMP1` hardware:

1. open a supplied browser URL
2. launch or focus Discord
3. join the selected call workflow
4. begin streaming the browser target with system audio

## Preconditions

- Windows 11 Pro installed on `COMP1`
- dedicated Discord account already logged in
- preferred browser installed
- desktop resolution and scaling standardized for selector tuning
- RDP or local monitor available for debugging

## Suggested Execution Order

### Step 1: Browser Launch Reliability

- verify URL validation
- verify process launch and browser focus
- verify the expected media page becomes visible

### Step 2: Discord Launch Detection

- verify Discord starts or focuses reliably
- verify the current user session is still active
- verify window title and process detection logic

### Step 3: Call Join Strategy

- capture UI screenshots for the target Discord flow
- determine whether guild/channel selection can be made from deep links, cached routes, or UI automation only
- log every automation step with timestamps

### Step 4: Stream Start Strategy

- choose whether to target a browser window, app window, or full display
- validate system-audio inclusion behavior with the chosen browser/content
- capture failure screenshots automatically

## Observability Requirements

- keep a rolling trace buffer in the `COMP1` agent state
- log the last successful step and the first failing step
- capture screenshot hooks during real selector work
- surface the trace to the `COMP2` desktop app

## Exit Criteria

The spike is considered successful when the `COMP2` app can send a URL, trigger the workflow, and either:

- complete browser + Discord + join + stream successfully, or
- fail with enough trace detail that developer-mode takeover can resume the session quickly

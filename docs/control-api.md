# Control API

## Authentication

### `POST /api/auth/login`

Request:

```json
{
  "username": "operator",
  "password": "replace-me"
}
```

Response:

```json
{
  "token": "opaque-session-token",
  "expiresAtUtc": "2026-07-03T23:00:00Z"
}
```

All authenticated requests require:

`Authorization: Bearer <token>`

## Health And State

### `GET /api/health`

Returns a summary used by the desktop app for polling.

### `GET /api/state`

Returns the current appliance state:

- `discordRunning`
- `browserRunning`
- `streamActive`
- `currentUrl`
- `lastError`
- `lastTraceEntries`

## Browser Control

### `POST /api/browser/open`

Request:

```json
{
  "url": "https://example.com/video"
}
```

Behavior:

- validates the URL
- launches the preferred browser on `COMP1`
- updates the tracked state

## Discord Control

### `POST /api/apps/discord/launch`

Starts or focuses Discord on `COMP1`.

### `POST /api/stream/start`

Request:

```json
{
  "browserUrl": "https://example.com/video",
  "discordGuildId": "optional-guild-id",
  "discordChannelId": "optional-channel-id",
  "channelDisplayName": "Friends Voice",
  "streamTarget": {
    "kind": "browser",
    "windowTitleHint": "YouTube"
  },
  "includeSystemAudio": true
}
```

Behavior:

- optionally opens the browser URL
- launches Discord
- attempts the join-call flow
- attempts to begin streaming the selected target
- records trace entries and updates state

### `POST /api/stream/stop`

Stops tracked stream state and records a trace entry. In v1 this is the orchestration stop signal; actual Discord UI teardown remains part of the automation spike.

## Developer Mode

### `POST /api/developer-mode/launch`

Returns the current remote-desktop launch details for `COMP2`.

Response:

```json
{
  "host": "192.168.1.50",
  "port": 3389,
  "mode": "rdp"
}
```

## LAN Device Bridge

### `POST /api/device-actions/http`

Reserved for future HTTP-based LAN device actions. This is not implemented in the initial scaffold, but the route family is intentionally reserved so later additions do not need to reshape the controller app navigation.

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
}

export interface BrowserOpenRequest {
  url: string;
}

export interface JoinVoiceChannelRequest {
  serverName: string;
  voiceChannelName: string;
}

export type StreamTargetKind = "browser" | "application" | "display";

export interface StreamTarget {
  kind: StreamTargetKind;
  windowTitleHint?: string;
  processNameHint?: string;
}

export interface StreamStartRequest {
  browserUrl?: string;
  discordGuildId?: string;
  discordChannelId?: string;
  discordServerName?: string;
  discordVoiceChannelName?: string;
  channelDisplayName?: string;
  streamTarget: StreamTarget;
  includeSystemAudio: boolean;
}

export interface TraceEntry {
  timestampUtc: string;
  level: "info" | "warning" | "error";
  message: string;
}

export interface ApplianceHealth {
  status: "healthy" | "degraded" | "error";
  discordRunning: boolean;
  browserRunning: boolean;
  streamActive: boolean;
}

export interface ApplianceState extends ApplianceHealth {
  currentUrl?: string;
  lastError?: string;
  selectedDiscordServer?: string;
  selectedDiscordChannel?: string;
  selectedStreamWindowTitle?: string;
  lastTraceEntries: TraceEntry[];
}

export interface DeveloperModeLaunchResponse {
  host: string;
  port: number;
  mode: "rdp";
}

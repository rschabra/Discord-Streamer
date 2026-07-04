import { contextBridge, ipcRenderer } from "electron";
import type {
  BrowserOpenRequest,
  LoginRequest,
  StreamStartRequest
} from "@discord-streamer/shared-contracts";

contextBridge.exposeInMainWorld("comp2Api", {
  connect: (baseUrl: string, request: LoginRequest) => ipcRenderer.invoke("session:connect", baseUrl, request),
  getHealth: () => ipcRenderer.invoke("state:health"),
  getState: () => ipcRenderer.invoke("state:full"),
  openBrowser: (request: BrowserOpenRequest) => ipcRenderer.invoke("browser:open", request),
  launchDiscord: () => ipcRenderer.invoke("discord:launch"),
  startStream: (request: StreamStartRequest) => ipcRenderer.invoke("stream:start", request),
  stopStream: () => ipcRenderer.invoke("stream:stop"),
  launchDeveloperMode: () => ipcRenderer.invoke("developer-mode:launch")
});

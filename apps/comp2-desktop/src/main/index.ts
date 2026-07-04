import { app, BrowserWindow, ipcMain } from "electron";
import { execFile } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";
import type {
  BrowserOpenRequest,
  JoinVoiceChannelRequest,
  LoginRequest,
  StreamStartRequest
} from "@discord-streamer/shared-contracts";
import { Comp1Client } from "./comp1Client.js";

const execFileAsync = promisify(execFile);
const currentDir = path.dirname(fileURLToPath(import.meta.url));

let client: Comp1Client | null = null;

function createWindow(): void {
  const window = new BrowserWindow({
    width: 1200,
    height: 900,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false,
      preload: path.join(currentDir, "..", "..", "src", "preload", "index.cjs")
    }
  });

  window.loadFile(path.join(currentDir, "..", "..", "src", "renderer", "index.html"));
}

app.whenReady().then(() => {
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});

ipcMain.handle("session:connect", async (_event, baseUrl: string, request: LoginRequest) => {
  client = new Comp1Client(baseUrl);
  return client.login(request);
});

ipcMain.handle("state:health", async () => {
  assertClient();
  return client!.getHealth();
});

ipcMain.handle("state:full", async () => {
  assertClient();
  return client!.getState();
});

ipcMain.handle("browser:open", async (_event, request: BrowserOpenRequest) => {
  assertClient();
  return client!.openBrowser(request);
});

ipcMain.handle("discord:launch", async () => {
  assertClient();
  return client!.launchDiscord();
});

ipcMain.handle("discord:joinVoice", async (_event, request: JoinVoiceChannelRequest) => {
  assertClient();
  return client!.joinVoiceChannel(request);
});

ipcMain.handle("stream:start", async (_event, request: StreamStartRequest) => {
  assertClient();
  return client!.startStream(request);
});

ipcMain.handle("stream:stop", async () => {
  assertClient();
  return client!.stopStream();
});

ipcMain.handle("developer-mode:launch", async () => {
  assertClient();
  const response = await client!.launchDeveloperMode();
  const target = `${response.host}:${response.port}`;
  await execFileAsync("mstsc", [`/v:${target}`]);
  return response;
});

function assertClient(): void {
  if (!client) {
    throw new Error("Connect to COMP1 first.");
  }
}

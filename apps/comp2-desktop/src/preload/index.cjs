const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("comp2Api", {
  connect: (baseUrl, request) => ipcRenderer.invoke("session:connect", baseUrl, request),
  getHealth: () => ipcRenderer.invoke("state:health"),
  getState: () => ipcRenderer.invoke("state:full"),
  openBrowser: (request) => ipcRenderer.invoke("browser:open", request),
  launchDiscord: () => ipcRenderer.invoke("discord:launch"),
  joinVoiceChannel: (request) => ipcRenderer.invoke("discord:joinVoice", request),
  startStream: (request) => ipcRenderer.invoke("stream:start", request),
  stopStream: () => ipcRenderer.invoke("stream:stop"),
  launchDeveloperMode: () => ipcRenderer.invoke("developer-mode:launch")
});

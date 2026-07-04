import type {
  BrowserOpenRequest,
  StreamStartRequest
} from "@discord-streamer/shared-contracts";

type Comp2Api = {
  connect(baseUrl: string, request: { username: string; password: string }): Promise<unknown>;
  getHealth(): Promise<unknown>;
  getState(): Promise<unknown>;
  openBrowser(request: BrowserOpenRequest): Promise<unknown>;
  launchDiscord(): Promise<unknown>;
  startStream(request: StreamStartRequest): Promise<unknown>;
  stopStream(): Promise<unknown>;
  launchDeveloperMode(): Promise<unknown>;
};

declare global {
  interface Window {
    comp2Api?: Comp2Api;
  }
}

const statusOutput = document.querySelector<HTMLPreElement>("#statusOutput")!;
const baseUrlInput = document.querySelector<HTMLInputElement>("#baseUrl")!;
const usernameInput = document.querySelector<HTMLInputElement>("#username")!;
const passwordInput = document.querySelector<HTMLInputElement>("#password")!;
const browserUrlInput = document.querySelector<HTMLInputElement>("#browserUrl")!;
const channelDisplayNameInput = document.querySelector<HTMLInputElement>("#channelDisplayName")!;
const windowTitleHintInput = document.querySelector<HTMLInputElement>("#windowTitleHint")!;

document.querySelector<HTMLButtonElement>("#connectButton")!.addEventListener("click", async () => {
  await run(async () => {
    const response = await getApi().connect(baseUrlInput.value, {
      username: usernameInput.value,
      password: passwordInput.value
    });
    print(response);
  });
});

document.querySelector<HTMLButtonElement>("#openBrowserButton")!.addEventListener("click", async () => {
  await run(async () => {
    const response = await getApi().openBrowser({
      url: browserUrlInput.value
    });
    print(response);
  });
});

document.querySelector<HTMLButtonElement>("#launchDiscordButton")!.addEventListener("click", async () => {
  await run(async () => {
    print(await getApi().launchDiscord());
  });
});

document.querySelector<HTMLButtonElement>("#startStreamButton")!.addEventListener("click", async () => {
  await run(async () => {
    const request: StreamStartRequest = {
      browserUrl: browserUrlInput.value || undefined,
      channelDisplayName: channelDisplayNameInput.value || undefined,
      includeSystemAudio: true,
      streamTarget: {
        kind: "browser",
        windowTitleHint: windowTitleHintInput.value || undefined
      }
    };

    print(await getApi().startStream(request));
  });
});

document.querySelector<HTMLButtonElement>("#stopStreamButton")!.addEventListener("click", async () => {
  await run(async () => {
    print(await getApi().stopStream());
  });
});

document.querySelector<HTMLButtonElement>("#developerModeButton")!.addEventListener("click", async () => {
  await run(async () => {
    print(await getApi().launchDeveloperMode());
  });
});

document.querySelector<HTMLButtonElement>("#refreshStateButton")!.addEventListener("click", async () => {
  await run(async () => {
    print(await getApi().getState());
  });
});

function print(value: unknown): void {
  statusOutput.textContent = JSON.stringify(value, null, 2);
}

function getApi(): Comp2Api {
  if (!window.comp2Api) {
    throw new Error("COMP2 bridge failed to load. Restart the Electron app after rebuilding it.");
  }

  return window.comp2Api;
}

async function run(action: () => Promise<void>): Promise<void> {
  try {
    await action();
  } catch (error) {
    statusOutput.textContent = error instanceof Error ? error.message : String(error);
  }
}

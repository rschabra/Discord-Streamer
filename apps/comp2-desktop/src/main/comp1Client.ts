import type {
  ApplianceHealth,
  ApplianceState,
  BrowserOpenRequest,
  DeveloperModeLaunchResponse,
  LoginRequest,
  LoginResponse,
  StreamStartRequest
} from "@discord-streamer/shared-contracts";

export class Comp1Client {
  private readonly baseUrl: string;
  private token: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/+$/, "");
  }

  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${this.baseUrl}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      throw new Error(`Login failed with status ${response.status}.`);
    }

    const payload = (await response.json()) as LoginResponse;
    this.token = payload.token;
    return payload;
  }

  async getHealth(): Promise<ApplianceHealth> {
    return this.request<ApplianceHealth>("/api/health", { method: "GET" });
  }

  async getState(): Promise<ApplianceState> {
    return this.request<ApplianceState>("/api/state", { method: "GET" });
  }

  async openBrowser(request: BrowserOpenRequest): Promise<ApplianceState> {
    return this.request<ApplianceState>("/api/browser/open", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  async launchDiscord(): Promise<ApplianceState> {
    return this.request<ApplianceState>("/api/apps/discord/launch", {
      method: "POST"
    });
  }

  async startStream(request: StreamStartRequest): Promise<ApplianceState> {
    return this.request<ApplianceState>("/api/stream/start", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  async stopStream(): Promise<ApplianceState> {
    return this.request<ApplianceState>("/api/stream/stop", {
      method: "POST"
    });
  }

  async launchDeveloperMode(): Promise<DeveloperModeLaunchResponse> {
    return this.request<DeveloperModeLaunchResponse>("/api/developer-mode/launch", {
      method: "POST"
    });
  }

  private async request<T>(path: string, init: RequestInit): Promise<T> {
    if (!this.token) {
      throw new Error("Not authenticated.");
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${this.token}`,
        ...(init.headers ?? {})
      }
    });

    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || `Request failed with status ${response.status}.`);
    }

    return (await response.json()) as T;
  }
}

import type { StateCreator } from "zustand";
import { isApiError } from "~/appState/apiError";
import type UserAccount from "~/appState/UserAccount";
import type { ApiTokenResponse } from "./ApiTokenResponse";
import type { AppStore } from "./AppStore";

/**
 * Minimal GIS type stubs (enough
 * for strong TS safety without an
 * external @types package)
 */
type CodeResponse = { code: string };

interface CodeClient {
  requestCode(): void;
}

export interface WindowWithGoogle extends Window {
  google?: {
    accounts: {
      oauth2: {
        initCodeClient(cfg: {
          client_id: string;
          scope?: string;
          ux_mode?: 'popup' | 'redirect';
          callback: (r: CodeResponse) => void;
          error_callback?: (err: unknown) => void;
        }): CodeClient;
      };
    };
  };
}

export interface AppAuthSlice {
  activeUser: UserAccount | null;
  apiAccessToken: string | null;
  isProcessingLogin: boolean;
  isGsiSdkReady: boolean;
  googleSdkClient: CodeClient | null;

  authenticateWithGoogle: () => Promise<boolean>;
  processGoogleAuthCode: (authCode: string) => Promise<boolean>;
  retrieveAccessToken: () => Promise<string | null>;
  refreshAccessToken: () => Promise<string | null>;
}

export const createAppAuthSlice: StateCreator<AppStore, [], [], AppAuthSlice> = (set, get) => ({
  activeUser: null,
  apiAccessToken: null,
  isProcessingLogin: false,
  isGsiSdkReady: false,
  googleSdkClient: null,

  processGoogleAuthCode: async (authCode: string) => {
    const accessTokenResponse = await get().sendUnauthenticatedRequest<string, ApiTokenResponse>(
      "user/google/apitoken",
      "POST",
      authCode,
      true
    );

    if (!isApiError(accessTokenResponse)) {
      set({
        apiAccessToken: accessTokenResponse.accessToken,
      });

      return true;
    }

    return false;
  },

  authenticateWithGoogle: async () => {
    const googleSdkClient = get().googleSdkClient;
    const isGsiSdkReady = get().isGsiSdkReady;
    
    if (!isGsiSdkReady || !googleSdkClient) {
      return false;
    }

    googleSdkClient.requestCode();

    return true;
  },

  retrieveAccessToken: async () => {
    return get().apiAccessToken;
  },

  refreshAccessToken: async () => {
    // TODO need to retrieve ID Token so we can confirm API and OAuth session are for the same identity.
    // TODO consider merging with retrieveAccessToken. This will need tracking of token expiration so we can do silent refresh.
    const currentAccessToken = get().apiAccessToken;

    const refreshResponse = await get().sendUnauthenticatedRequest<any, ApiTokenResponse>("user/refresh-token",
      "POST",
      {
        accessToken: currentAccessToken,
        idToken: "id-token-here-wip",
        identityProvider: "Google"
      },
      true
    );

    if (!isApiError(refreshResponse)) {
      set({
        apiAccessToken: refreshResponse.accessToken
      });
      return refreshResponse.accessToken;
    }

    return null;
  }
});


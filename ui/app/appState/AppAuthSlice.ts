import type { StateCreator } from "zustand";
import { isApiError } from "~/appState/apiError";
import type UserAccount from "~/appState/UserAccount";
import type { ApiTokenResponse } from "./ApiTokenResponse";
import type { AppStore } from "./AppStore";
import type { CodeClient } from "./GoogleAuthService";

export const guestUser: UserAccount = {
  player: {
    playerId: -1,
    playerName: "Guest User",
  },
  isAuthenticated: false
};

export interface AppAuthSlice {
  activeUser: UserAccount;
  apiAccessToken: string | null;
  isProcessingLogin: boolean;
  isGsiSdkReady: boolean;
  googleSdkAuthCodeClient: CodeClient | null;

  authenticateWithGoogle: () => Promise<boolean>;
  signOut: () => void;
  processGoogleAuthCode: (authCode: string) => Promise<boolean>;
  retrieveAccessToken: () => Promise<string | null>;
  refreshAccessToken: () => Promise<string | null>;
}

export const createAppAuthSlice: StateCreator<AppStore, [], [], AppAuthSlice> = (set, get) => ({
  activeUser: guestUser,
  apiAccessToken: null,
  isProcessingLogin: false,
  isGsiSdkReady: false,
  googleSdkAuthCodeClient: null,

  processGoogleAuthCode: async (authCode: string) => {
    const accessTokenResponse = await get().sendUnauthenticatedRequest<ApiTokenResponse>(
      "user/google/apitoken",
      "POST",
      authCode,
      true
    );

    if (isApiError(accessTokenResponse)) {
      // we have failed to exchange google auth code for app access token.
      get().resetSessionState();
      return false;
    }

    set({
      apiAccessToken: accessTokenResponse.accessToken,
    });

    return true;
  },

  authenticateWithGoogle: async () => {
    const googleSdkClient = get().googleSdkAuthCodeClient;
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
  
  signOut: () => {
    get().resetSessionState();
  },

  refreshAccessToken: async () => {
    // TODO need to retrieve ID Token so we can confirm API and OAuth session are for the same identity.
    // TODO consider merging with retrieveAccessToken. This will need tracking of token expiration so we can do silent refresh.
    const currentAccessToken = get().apiAccessToken;

    const refreshResponse = await get().sendUnauthenticatedRequest<ApiTokenResponse>("user/refresh-token",
      "POST",
      {
        accessToken: currentAccessToken,
        idToken: "id-token-here-wip",
        identityProvider: "Google"
      },
      true,
      async _ => { /* noop */ }
    );

    if (isApiError(refreshResponse)) {
      // We were not able to retrieve fresh access token because of bad request params.
      // Otherwise, API is up and running and there's active and usable connection between API and UI.
      // We will assume this session is no longer valid so we'll reset the state.
      if (refreshResponse.status == 400) {
        get().resetSessionState();
      }
    } else {
      set({
        apiAccessToken: refreshResponse.accessToken
      });
      return refreshResponse.accessToken;
    }

    return null;
  }
});


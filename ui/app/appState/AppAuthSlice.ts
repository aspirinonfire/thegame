import type { StateCreator } from "zustand";
import { isApiError } from "~/appState/ApiError";
import type UserAccount from "~/appState/UserAccount";
import type { ApiTokenResponse } from "./ApiTokenResponse";
import type { AppStore } from "./AppStore";
import type { CodeClient, IdClient } from "./GoogleAuthService";

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
  googleSdkIdCodeClient: IdClient | null;

  signOut: () => void;

  exchangeIdTokenForApiAccessToken: (idToken: string) => Promise<boolean>;
  retrieveGoogleIdToken: () => Promise<string | null>;
  retrieveAccessToken: () => Promise<string | null>;
  refreshAccessToken: () => Promise<string | null>;
}

export const createAppAuthSlice: StateCreator<AppStore, [], [], AppAuthSlice> = (set, get) => ({
  activeUser: guestUser,
  apiAccessToken: null,
  isProcessingLogin: false,
  isGsiSdkReady: false,
  googleSdkAuthCodeClient: null,
  googleSdkIdCodeClient: null,

  exchangeIdTokenForApiAccessToken: async (idToken) => {
    set({
      isProcessingLogin: true
    });

    // TODO needs CSRF protection

    const accessTokenResponse = await get().sendUnauthenticatedRequest<ApiTokenResponse>(
      "user/google/apitoken",
      "POST",
      idToken,
      true
    );

    set({
      isProcessingLogin: false
    });

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

  retrieveGoogleIdToken: async () => {
    const idClient = get().googleSdkIdCodeClient;
    const isGsiSdkReady = get().isGsiSdkReady;
    
    if (!isGsiSdkReady || !idClient) {
      return null;
    }

    return await idClient.prompt();
  },

  retrieveAccessToken: async () => {
    return get().apiAccessToken;
  },
  
  signOut: () => {
    get().resetSessionState();
  },

  refreshAccessToken: async () => {
    const idToken = await get().retrieveGoogleIdToken();
    if (!idToken) {
      // TODO show login button
    }

    const refreshResponse = await get().sendUnauthenticatedRequest<ApiTokenResponse>("user/refresh-token",
      "POST",
      {
        idToken: idToken,
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


import type { StateCreator } from "zustand";
import { isApiError } from "~/common-components/apiError";
import type UserAccount from "~/game-core/UserAccount";
import type { ApiTokenResponse } from "./ApiTokenResponse";
import type { AppStore } from "./AppStore";

export interface AppAuthSlice {
  activeUser: UserAccount | null;
  apiAccessToken: string | null;

  authenticateWithGoogleAuthCode: (authCode: string) => Promise<boolean>;
  retrieveAccessToken: () => Promise<string | null>;
  refreshAccessToken: () => Promise<string | null>;
}

export const createAppAuthSlice: StateCreator<AppStore, [], [], AppAuthSlice> = (set, get) => ({
  activeUser: null,
  apiAccessToken: null,

  authenticateWithGoogleAuthCode: async (authCode) => {
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


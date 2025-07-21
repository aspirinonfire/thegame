import { useEffect, useRef } from "react";
import { useShallow } from "zustand/shallow";
import type { GsiButtonConfiguration } from "~/appState/GoogleAuthService";
import { useAppState } from "~/appState/useAppState";

const GoogleSignInButton = (buttonOptions: GsiButtonConfiguration) => {
  const googleSignInMountPoint = useRef<HTMLDivElement>(null);
  const [googleIdClient, exchangeIdTokenForApiAccessToken, retrievePlayerData] = useAppState(useShallow(state =>[
    state.googleSdkIdCodeClient,
    state.exchangeIdTokenForApiAccessToken,
    state.retrievePlayerData
  ]));

  useEffect(() => {
    if (!googleIdClient || !googleSignInMountPoint.current || googleSignInMountPoint.current.hasChildNodes()) {
      return;
    }

    googleIdClient
      .renderLoginButtonWithCredHandler(googleSignInMountPoint.current!, buttonOptions)
      .then((idToken) => {
        if (!idToken) {
          return Promise.resolve(false);
        }
        return exchangeIdTokenForApiAccessToken(idToken);
      })
      .then(hasToken => {
        if (hasToken) {
          return retrievePlayerData()
        } else {
          // TODO show error!
        }
      });
  }, [googleSignInMountPoint]);

  return <div id="login-wrapper" ref={googleSignInMountPoint} />;
}

export default GoogleSignInButton;
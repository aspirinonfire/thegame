import { useEffect, useRef, useCallback, useState } from 'react';
import { useAppStore } from './useAppStore';
import { useShallow } from 'zustand/shallow';

/**
 * Minimal GIS type stubs (enough
 * for strong TS safety without an
 * external @types package)
 */
type CodeResponse = { code: string };

interface CodeClient {
  requestCode(): void;
}

interface WindowWithGoogle extends Window {
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

export function useGoogleAuthCode() {
  // lazy one-time initialization of the Google Login
  const clientRef = useRef<CodeClient | null>(null);

  const [isGsiSdkReady, processGoogleAuthCode, retrievePlayerData] = useAppStore(useShallow(state => [
    state.isGsiSdkReady,
    state.authenticateWithGoogleAuthCode,
    state.retrievePlayerData
  ]));

  const [isProcessing, setIsProcessing] = useState(false);

  // initialize the Google SDK
  useEffect(() => {
    if (!isGsiSdkReady || clientRef.current ) {
      return;
    }

    const google = (window as WindowWithGoogle).google;
    if (!google?.accounts?.oauth2?.initCodeClient) {
      return; // SDK not loaded yet
    }

    if (!import.meta.env.VITE_GOOGLE_CLIENT_ID) {
      console.error("Google Client ID is missing. Auth is will not work!");
      return;
    }

    clientRef.current = google.accounts.oauth2.initCodeClient({
      client_id: import.meta.env.VITE_GOOGLE_CLIENT_ID,
      scope: 'openid email profile',
      ux_mode: 'popup',
      callback: ({ code }) => {
        processGoogleAuthCode(code)
          .then(_ => {
            return retrievePlayerData()
          })
          .finally(() => {
            setIsProcessing(false);
          });
      },
      error_callback: (err) => {
        setIsProcessing(false);
        console.error(err)  // handles user-closed popup etc.
      },
    });
  }, [isGsiSdkReady]);

  const login = () => {
    if (clientRef.current == null) {
      alert("SDK not ready yet. Please try again later.");
    } else {
      setIsProcessing(true);
      clientRef.current?.requestCode();
    }
  };

  return { login, isProcessing, isGsiSdkReady };
}
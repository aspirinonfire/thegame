/**
 * Minimal GIS type stubs (enough
 * for strong TS safety without an
 * external @types package)
 */
export type CodeResponse = { code: string };

export interface CodeClient {
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

export function InitializeGoogleAuthCodeClient(
  onRetrievedCode: (code: string) => Promise<void>,
  onError: (err: unknown) => Promise<void>
) {
  const google = (window as WindowWithGoogle).google;
  if (!google) {
    console.error("Google SDK did not load!!");
    return null;
  }

  return google.accounts.oauth2.initCodeClient({
    client_id: import.meta.env.VITE_GOOGLE_CLIENT_ID,
    scope: 'openid email profile',
    ux_mode: 'popup',
    callback: ({ code }) => onRetrievedCode(code),
    error_callback: (err) => onError(err),
  });
}
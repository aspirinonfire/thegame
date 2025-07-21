/**
 * Minimal GIS type stubs (enough
 * for strong TS safety without an
 * external @types package)
 */
export type CodeResponse = { code: string };

export interface CodeClient {
  requestCode(): void;
}

// https://developers.google.com/identity/oauth2/web/reference/js-reference
export interface CodeClientConfiguration {
  client_id: string;
  scope?: string;
  auto_select: boolean;
  ux_mode?: "popup" | "redirect";
  callback: (r: CodeResponse) => void;
  error_callback?: (err: unknown) => void;
}

export interface IdClient {
  prompt(): void;
}

export interface IdTokenCredential {
  credential: string
}

// https://developers.google.com/identity/gsi/web/reference/js-reference
export interface IdConfiguration {
  client_id: string;
  color_scheme: "default" | "dark" | "light";
  auto_select: boolean;
  button_auto_select: boolean;
  ux_mode: "popup" | "redirect";
  context?: "signin" | "signup" | "use";
  nonce?: string;
  itp_support?: boolean;
  login_hint?: string;
  use_fedcm_for_prompt?: boolean;
  use_fedcm_for_button?: boolean;
  allowed_parent_origin?: string | string[]
  callback: (r: IdTokenCredential) => void;
}

export interface WindowWithGoogle extends Window {
  google?: {
    accounts: {
      oauth2: {
        initCodeClient(cfg: CodeClientConfiguration): CodeClient;
      };

      id:  {
        initialize(cfg: IdConfiguration) : void;
        prompt: (notification: any) => void;
      }
    };
  };
}

export function InitializeGoogleAuthCodeClient(
  onRetrievedCode: (code: string) => Promise<void>,
  onError: (err: unknown) => Promise<void>
) : CodeClient | null {
  const google = (window as WindowWithGoogle).google;
  if (!google) {
    console.error("Google SDK did not load!!");
    return null;
  }

  return google.accounts.oauth2.initCodeClient({
    client_id: import.meta.env.VITE_GOOGLE_CLIENT_ID,
    scope: "openid email profile",
    ux_mode: "popup",
    auto_select: true,
    callback: ({ code }) => onRetrievedCode(code),
    error_callback: (err) => onError(err),
  });
};

export function InitializeGoogleIdTokenClient(
  onSuccess: (r: IdTokenCredential) => void
) : IdClient | null {
  const google = (window as WindowWithGoogle).google;
  if (!google) {
    console.error("Google SDK did not load!!");
    return null;
  }

  google.accounts.id.initialize({
    client_id: import.meta.env.VITE_GOOGLE_CLIENT_ID,
    auto_select: true,
    button_auto_select: true,
    use_fedcm_for_prompt: true,
    use_fedcm_for_button: true,
    color_scheme: "dark",
    ux_mode: "popup",
    callback: (r) => onSuccess(r),
  });

  return {
    prompt: () => {
      // important! this removes one-tap cooldown in case user clicks outside of popup.
      (window as any).cookieStore?.delete("g_state");
      google.accounts.id.prompt((noti: any) => {
        // Dev Note: uncomment to for One-Tap debugging
        // console.log({
        //   isDismissed: noti?.isDismissedMoment(),
        //   dismissReason: noti?.getDismissedReason(),
        //   isDisplayed: noti?.isDisplayed(),
        //   notDisplayedReason: noti?.getNotDisplayedReason(),
        //   isSkipped: noti?.isSkippedMoment(),
        //   skippedReason: noti?.getSkippedReason()
        // });
      });
    }
  };
};
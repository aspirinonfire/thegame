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
  login_hint?: string,
  callback: (r: CodeResponse) => void;
  error_callback?: (err: unknown) => void;
}

export interface IdClient {
  prompt(): Promise<string | null>;
  renderLoginButtonWithCredHandler: (parent: HTMLElement, btnConfig: GsiButtonConfiguration) => Promise<string | null>;
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
  prompt_parent_id?: string;
  use_fedcm_for_prompt?: boolean;
  use_fedcm_for_button?: boolean;
  allowed_parent_origin?: string | string[]
  callback: (r: IdTokenCredential) => void;
}

export interface GsiButtonConfiguration {
  type: "icon" | "standard",
  theme?: "outline" | "filled_blue" | "filled_black",
  size?: "small" | "medium" | "large",
  text?: "signin_with" | "signup_with" | "continue_with" | "signin",
  shape?: "rectangular" | "pill" | "circle" | "square",
  logo_alignment?: "left" | "center"
  width?: string,
  click_listener?: (e: any) => void
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
        renderButton: (parent: HTMLElement, btnConfig: GsiButtonConfiguration) => void;
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

const idTokenQueue: {
  resolve: (token: string | null) => void;
  timer: ReturnType<typeof setTimeout>;
}[] = [];

export function InitializeGoogleIdTokenClient() : IdClient | null {
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
    itp_support: true,
    color_scheme: "dark",
    ux_mode: "popup",
    prompt_parent_id: "app-header",
    callback: (idTokenResponse) => {
      let job = idTokenQueue.shift();
      while (!!job) {
        clearTimeout(job.timer);
        job.resolve(idTokenResponse?.credential);
        job = idTokenQueue.shift();
      }
    }
  });

  return {
    prompt: () => {
      const idTokenProm = new Promise<string | null>(resolve => {
        // push to internal queue to be resolved in callback defined in initialize
        idTokenQueue.push({
          resolve: resolve,
          timer: setTimeout(() => resolve(null), 10_000)
        });
        
        google.accounts.id.prompt((noti: any) => {
          if (noti?.getDismissedReason() == "credential_returned") {
            let job = idTokenQueue.shift();
            while (!!job) {
              clearTimeout(job.timer);
              job.resolve(null);
              job = idTokenQueue.shift();
            }
          } else {
            // TODO show login button when prompt fails
            
            console.log({
              isDismissed: noti?.isDismissedMoment(),
              dismissReason: noti?.getDismissedReason(),
              isDisplayed: noti?.isDisplayed(),
              notDisplayedReason: noti?.getNotDisplayedReason(),
              isSkipped: noti?.isSkippedMoment(),
              skippedReason: noti?.getSkippedReason()
            });
          }
        });
      });

      return idTokenProm;
    },
    
    renderLoginButtonWithCredHandler: (parent, btnCfg) => {
      return new Promise<string | null>(resolve => {
        const btnConfigWithListener: GsiButtonConfiguration = {
          ...btnCfg,
          click_listener: (e) => {
            btnCfg.click_listener?.apply(null, e);
            
            // Push to internal queue as soon as user clicks the button.
            // Promise will be resolved in callback defined in initialize.
            // Wait is bit longer (compared to prompt implementation) to give user time to interract with popup
            idTokenQueue.push({
              resolve: resolve,
              timer: setTimeout(() => resolve(null), 60_000)
            });
          }
        };

        google.accounts.id.renderButton(parent, btnConfigWithListener);
      });
    }
  };
};
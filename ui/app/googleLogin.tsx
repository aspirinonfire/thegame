import { CredentialResponse, GoogleLogin, useGoogleLogin } from "@react-oauth/google";
import { authTokenKey, SetLocalStorage } from "./appUtils";

interface ApiTokenResponse {
  accessToken: string
}

interface GoogleSignInArgs {
  raiseSignedInEvent: () => void
}

export default function GoogleSignIn({ raiseSignedInEvent } : GoogleSignInArgs) {
  async function onLoginSuccess(response: CredentialResponse) {
    const requestParams : RequestInit = {
      cache: "no-cache",
      method: "POST",
      headers: {
        "Content-Type": "application/json; charset=utf-8",
      },
      body: JSON.stringify(response.credential),
    };

    const accessTokenResponse = await fetch("/api/user/token", requestParams);

    const responseBody = await accessTokenResponse.json() as ApiTokenResponse;

    if (accessTokenResponse.status == 200) {
      SetLocalStorage(authTokenKey, responseBody.accessToken);
      raiseSignedInEvent();
      return;
    }
    
    console.error(`Failed to retrieve API token ${accessTokenResponse.status}: ${responseBody}`);
  }

  return (
    <GoogleLogin onSuccess={async (creds) => await onLoginSuccess(creds)} />
  )
}


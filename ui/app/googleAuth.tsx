import { CredentialResponse, GoogleLogin } from "@react-oauth/google";
import { authTokenKey, setLocalStorage } from "./appUtils";
import { useContext } from "react";
import { CurrentUserAccountContext } from "./common/gameCore/gameContext";

interface ApiTokenResponse {
  accessToken: string
}

interface GoogleSignInArgs {
  raiseSignedInEvent: () => void
}

export default function GoogleAuth({ raiseSignedInEvent } : GoogleSignInArgs) {
  const { userDetails } = useContext(CurrentUserAccountContext);

  async function onLoginSuccess(response: CredentialResponse) {
    const requestParams : RequestInit = {
      cache: "no-cache",
      method: "POST",
      headers: {
        "Content-Type": "application/json; charset=utf-8",
      },
      body: JSON.stringify(response.credential),
    };

    const accessTokenResponse = await fetch("/api/user/google/apitoken", requestParams);

    const responseBody = await accessTokenResponse.json() as ApiTokenResponse;

    if (accessTokenResponse.status == 200) {
      setLocalStorage(authTokenKey, responseBody.accessToken);
      raiseSignedInEvent();
      return;
    }
    
    console.error(`Failed to retrieve API token ${accessTokenResponse.status}: ${responseBody}`);
  }

  function onSignOut() {
    setLocalStorage(authTokenKey, null);
    raiseSignedInEvent();
  }

  return (
    <>
      {
        userDetails.isAuthenticated ?
          <p className="hover:cursor-pointer" onClick={onSignOut}>Sign Out</p> :
          <GoogleLogin onSuccess={async (creds) => await onLoginSuccess(creds)}
            type="standard"
            theme="filled_black"
            size="medium"
            shape="square"
            ux_mode="popup"
            use_fedcm_for_prompt={true}/>
      }
    </>
  )
}


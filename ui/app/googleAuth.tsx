import { useGoogleLogin } from "@react-oauth/google";
import { ApiTokenResponse, authTokenKey, setLocalStorage } from "./appUtils";
import { useContext } from "react";
import { CurrentUserAccountContext } from "./common/gameCore/gameContext";


interface GoogleSignInArgs {
  raiseSignedInEvent: () => void
}

export default function GoogleAuth({ raiseSignedInEvent } : GoogleSignInArgs) {
  const { userDetails } = useContext(CurrentUserAccountContext);
  
  const googleLogin = useGoogleLogin({
    flow: "auth-code",
    ux_mode: "popup",
    onSuccess: async (codeResponse) => {
      const requestParams : RequestInit = {
        cache: "no-cache",
        method: "POST",
        headers: {
          "Content-Type": "application/json; charset=utf-8",
        },
        body: JSON.stringify(codeResponse.code),
      };
  
      try {
        const accessTokenResponse = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/user/google/apitoken`, requestParams);
  
        const responseBody = await accessTokenResponse.json() as ApiTokenResponse;
    
        if (accessTokenResponse.status == 200) {
          setLocalStorage(authTokenKey, responseBody.accessToken);
          raiseSignedInEvent();
          return;
        }
        
        console.error(`Failed to retrieve API token ${accessTokenResponse.status}: ${responseBody}`);
      } catch (error) {
        console.log(error);
      }
    },
    onError: error => console.log(error)
  })

  function onSignOut() {
    setLocalStorage(authTokenKey, null);
    raiseSignedInEvent();
  }

  return (
    <>
      {
        userDetails.isAuthenticated ?
          <p className="hover:cursor-pointer" onClick={onSignOut}>Sign Out</p> :
          <div className="flex flex-row items-center text-gray-200 bg-blue-900 hover:bg-blue-950 p-2" onClick={() => googleLogin()}>
            Sign in with Google
          </div>
      }
    </>
  )
}


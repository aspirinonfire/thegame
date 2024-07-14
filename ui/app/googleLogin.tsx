import { useGoogleLogin } from "@react-oauth/google";
import { authTokenKey, SetLocalStorage } from "./appUtils";
import { useRouter } from "next/navigation";

interface ApiTokenResponse {
  apiToken: string
}

export default function GoogleSignIn() {
  const router = useRouter();

  const googleLogin = useGoogleLogin({
    onSuccess: async tokenResponse => {
      const accessTokenResponse = await fetch("/api/user/token", {
        cache: "no-cache",
        method: "POST",
        headers: {
          "Content-Type": "application/json; charset=utf-8",
        },
        body: JSON.stringify(tokenResponse.access_token),
      });

      const responseBody = await accessTokenResponse.json() as ApiTokenResponse; 

      if (accessTokenResponse.status != 200) {
        console.error(`Failed to retrieve API token ${accessTokenResponse.status}: ${responseBody}`)
        return;
      }

      SetLocalStorage(authTokenKey, responseBody.apiToken);
      router.push("/")
    },
    onError: errorResponse => console.log(errorResponse),
    flow: "implicit",
    include_granted_scopes: false
  });

  return (
    // TODO make it prettier
    <div className="flex flex-row items-center text-gray-200 bg-blue-900 hover:bg-blue-950 p-2" onClick={() => googleLogin()}>
      Login with Google 🚀
    </div>
  )
}


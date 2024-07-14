import { useGoogleLogin } from "@react-oauth/google";

export default function GoogleSignIn() {
  const googleLogin = useGoogleLogin({
    flow: "auth-code",
    ux_mode: "redirect",
    redirect_uri: "https://localhost:8080/account/signin-google"
  });

  return (
    // TODO make it prettier
    <div className="flex flex-row items-center text-gray-200 bg-blue-900 hover:bg-blue-950 p-2" onClick={() => googleLogin()}>
      Login with Google 🚀
    </div>
  )
}


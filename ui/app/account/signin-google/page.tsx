"use client"
import { authTokenKey, SetLocalStorage } from "@/app/appUtils";
import { useSearchParams, useRouter } from "next/navigation";
import { useEffect } from "react";

interface ApiTokenResponse {
  apiToken: string
}

export default function SignInGoogle() {
  const searchParams = useSearchParams();
  const router = useRouter();

  const authCode = searchParams.get("code");

  console.log(authCode);

  useEffect(() => {
    async function getNewToken() {
      const accessTokenResponse = await fetch("/api/user/token", {
        cache: "no-cache",
        method: "POST",
        headers: {
          "Content-Type": "application/json; charset=utf-8",
        },
        body: JSON.stringify(authCode),
      });

      try {
        const responseBody = await accessTokenResponse.json() as ApiTokenResponse; 
        if (accessTokenResponse.status != 200) {
          console.error(`Failed to retrieve API token ${accessTokenResponse.status}: ${responseBody}`)
        } else {
          SetLocalStorage(authTokenKey, responseBody.apiToken);
        }
      } catch (ex) {
        console.error(ex);
      }
      
      router.push("/");
    }
    
    if (!authCode) {
      router.push("/");
      return;
    }

    getNewToken();
  });
}
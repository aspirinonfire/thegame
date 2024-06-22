"use client"
import { useContext, useEffect } from "react";
import { useRouter } from "next/navigation";
import { GetCurrentGame } from "./common/gameCore/gameRepository";

// page.tsx is a special Next.js file that exports a React component, and it's required for the route to be accessible
// this page will automatically redirect to a game when current game is present, otherwise it will redirect to a history page
export default function Home() {
  const router = useRouter();

  useEffect(() => {
    async function redirectToGameOrHistory() {
      const currentGame = await GetCurrentGame();

      const redirectPage = !currentGame ? "/history" : "/game";

      router.push(redirectPage);
    }
    redirectToGameOrHistory();
  });
}

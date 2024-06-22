"use client"
import { useContext, useEffect } from "react";
import { CurrentGameContext } from "./common/gameCore/gameContext";
import { useRouter } from "next/navigation";

// page.tsx is a special Next.js file that exports a React component, and it's required for the route to be accessible
// this page will automatically redirect to a game when current game is present, otherwise it will redirect to a history page
export default function Home() {
  const { currentGame } = useContext(CurrentGameContext);
  const router = useRouter();
  router.refresh();

  useEffect(() => {
    const redirectPage = !currentGame ? "/history" : "/game";

    router.push(redirectPage);
  });
}

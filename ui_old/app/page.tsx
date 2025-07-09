"use client"
import { useContext, useEffect } from "react";
import { useRouter } from "next/navigation";
import { CurrentGameContext } from "./common/gameCore/gameContext";

// page.tsx is a special Next.js file that exports a React component, and it's required for the route to be accessible
// this page will automatically redirect to a game when current game is present, otherwise it will redirect to a history page
export default function Home() {
  const router = useRouter();
  const { activeGame } = useContext(CurrentGameContext);

  // use effect to ensure redirect happens on a new stack frame to avoid update-while-rendering errors
  useEffect(() => {
    const redirectPage = !activeGame ? "/history" : "/game";

    router.push(redirectPage);
  });
}

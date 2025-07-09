"use client"
import { Inter } from "next/font/google";
import "./globals.css";
import { useEffect, useState } from "react";
import { GetAccount, RetrieveActiveGame } from "./common/gameCore/gameRepository";
import { CurrentGameContext, CurrentUserAccountContext } from "./common/gameCore/gameContext";
import { Spinner } from "flowbite-react";
import { Game } from "./common/gameCore/gameModels";
import { GoogleOAuthProvider } from "@react-oauth/google";
import GameNavBar from "./navbar";
import { UserDetails } from "./common/accounts";

const inter = Inter({ subsets: ["latin"] });

function getDefaultUserDetails(): UserDetails {
  return {
    isAuthenticated: false,
    Player: {
      playerName: "Guest",
      playerId: -1
    }
  };
}

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode; }>) {
  // track user account, and current game.
  // presence of a current game will redirect index to game route.
  // current game will also be used directly on the game page.
  const [needsFetch, setNeedsFetch] = useState(true);
  const [userDetails, setUserDetails] = useState<UserDetails>(getDefaultUserDetails());
  const [activeGame, setActiveGame] = useState<Game | null>(null);

  const [isDrawerMenuOpen, setIsDrawerMenuOpen] = useState(false);

  // fetch player, and game data
  useEffect(() => {
    async function FetchData() {
      const [player, game] = await Promise.all([GetAccount(), RetrieveActiveGame()]);

      if (player != null) {
        setUserDetails({
          isAuthenticated: true,
          Player: player
        });
      } else {
        setUserDetails(getDefaultUserDetails());
      }

      setActiveGame(game);
      setNeedsFetch(false);
    }

    setIsDrawerMenuOpen(false);

    if (needsFetch) {
      FetchData();
    }
  }, [needsFetch]);

  return (
    <html lang="en">
      <head>
        <title>The License Plate Game</title>
        <base href="/" />
        <link rel="icon" type="image/x-icon" href="favicon.ico" />
        {/* Enable PWA */}
        <link rel="manifest" href="manifest.json" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta charSet="utf-8" />
        <meta name="color-scheme" content="light only" />
      </head>
      <body className={inter.className}>
        <GoogleOAuthProvider clientId={process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? ''}>
          <CurrentUserAccountContext.Provider value={{userDetails, setUserDetails}}>
            <CurrentGameContext.Provider value={{ activeGame, setActiveGame }}>
              <div className="flex flex-col min-h-screen">
                <header className="flex-row rounded-lg bg-gradient-to-r from-gray-900 to-slate-700 p-4">
                  <GameNavBar isDrawerMenuOpen={isDrawerMenuOpen}
                     setIsDrawerMenuOpen={setIsDrawerMenuOpen} 
                     setNeedsDataRefetch={setNeedsFetch}/>
                </header>
                <main className={`flex flex-col flex-grow mt-4 rounded-lg bg-gradient-to-bl from-10% from-slate-700 to-gray-900 text-gray-300 p-4 transition-all ${isDrawerMenuOpen ? 'blur-sm' : ''}`}>
                  {needsFetch ?
                    (<div className="flex items-center justify-center m-auto">
                      <Spinner color="info" className="h-20 w-20" />
                    </div>) :
                    (<>{children}</>)}
                </main>
              </div>
            </CurrentGameContext.Provider>
          </CurrentUserAccountContext.Provider>
        </GoogleOAuthProvider>
      </body>
    </html>);
}

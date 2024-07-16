"use client"
import { Inter } from "next/font/google";
import "./globals.css";
import { useEffect, useState } from "react";
import { GetAccount, GetCurrentGame } from "./common/gameCore/gameRepository";
import { CurrentGameContext, CurrentUserAccountContext, GameContext } from "./common/gameCore/gameContext";
import UserAccount from "./common/accounts";
import { Spinner } from "flowbite-react";
import { Game } from "./common/gameCore/gameModels";
import { GoogleOAuthProvider } from "@react-oauth/google";
import GameNavBar from "./navbar";
import refreshOnNewVersion from "./appUtils";

const inter = Inter({ subsets: ["latin"] });

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {

  // track user account, and current game.
  // presence of a current game will redirect index to game route.
  // current game will also be used directly on the game page.
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [userAccount, setUserAccount] = useState<UserAccount | null>(null);
  const [activeGame, setActiveGame] = useState<Game | null>(null);
  const [isDrawerMenuOpen, setIsDrawerMenuOpen] = useState(false);

  // track is fetching
  const [needsFetch, setNeedsFetch] = useState(true);

  // fetch user account, and game data if not tracked
  useEffect(() => {
    async function FetchData() {
      const [account, game] = await Promise.all([GetAccount(), GetCurrentGame()]);

      setUserAccount(account);
      setActiveGame(game);
      setNeedsFetch(false);
    }

    if (needsFetch) {
      FetchData();
    }
  }, [needsFetch]);

  // register autorefresh on new version
  useEffect(() => refreshOnNewVersion());

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
          <CurrentUserAccountContext.Provider value={userAccount}>
            <CurrentGameContext.Provider value={{ activeGame, setActiveGame }}>
              <div className="flex flex-col min-h-screen">
                <header className="flex-row rounded-lg bg-gradient-to-r from-gray-900 to-slate-700 p-4">
                  <GameNavBar isAuthenticated={isAuthenticated}
                     isDrawerMenuOpen={isDrawerMenuOpen}
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

"use client"
import { Inter } from "next/font/google";
import "./globals.css";
import Link from 'next/link';
import { useEffect, useState } from "react";
import { GetAccount, GetCurrentGame } from "./common/gameCore/gameRepository";
import { CurrentGameContext, CurrentUserAccountContext } from "./common/gameCore/gameContext";
import UserAccount from "./common/accounts";
import { Game } from "./common/gameCore/gameModels";

const inter = Inter({ subsets: ["latin"] });


export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  // track user account, and current game.
  // presence of a current game will redirect index to game route.
  // current game will also be used directly on the game page.
  const [userAccount, setUserAccount] = useState<UserAccount | null>(null);
  const [currentGame, setCurrentGame] = useState<Game | null>(null);

  // track is fetching
  const [needsFetch, setNeedsFetch] = useState(true);

  // fetch user account, and game data if not tracked
  useEffect(() => {
    async function FetchData() {
      if (!needsFetch) {
        return;
      }
      const [fetchedUserAccount, fetchedCurrentGame] =  await Promise.all([
        GetAccount(),
        GetCurrentGame()
      ]);

      setUserAccount(fetchedUserAccount);
      setCurrentGame(fetchedCurrentGame);
      setNeedsFetch(false);
    }

    FetchData();
  });

  const showFetchedContent = () =>
    <div className="flex flex-col gap-6 rounded-lg bg-gray-100">
      <div className="px-4 py-4 h-full">
        {children}
      </div>
    </div>

  const showIsFetching = () =>
    <div className="flex flex-col gap-6 rounded-lg bg-gray-100">
      <div className="px-4 py-4 text-gray-800">
        ...Fetching Data
      </div>
    </div>
  
  return (
    <html lang="en">
      <body className={inter.className}>
        <CurrentUserAccountContext.Provider value={userAccount}>
          <CurrentGameContext.Provider value={{
             currentGame,
             setNewCurrentGame: (newCurrentGame) => {
              setCurrentGame(newCurrentGame);
             }}}>

            <div className="flex-col min-h-screen">
              <header className="flex h-20 shrink-0 items-end rounded-lg bg-blue-500 p-4">
                <nav className="inline-flex items-center gap-20">
                  <h1 className="text-2xl">License Plate Game</h1>
                  <div className="inline-flex items-center gap-4">
                    <Link href="/game">Game</Link>
                    <Link href="/history">History</Link>
                    <Link href="/about">About</Link>
                    <a className="text white p-4">Share App</a>
                  </div>
                </nav>
              </header>

              <main className="flex flex-row grow">
                <div className="mt-4 grow">
                  { needsFetch ? showIsFetching() : showFetchedContent() }
                </div>
              </main>
            </div>
          </CurrentGameContext.Provider>
        </CurrentUserAccountContext.Provider>
      </body>
    </html>
  );
}

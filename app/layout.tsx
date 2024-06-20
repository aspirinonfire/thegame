"use client"
import { Inter } from "next/font/google";
import "./globals.css";
import Link from 'next/link';
import { usePathname } from 'next/navigation'
import { useEffect, useState } from "react";
import { GetAccount, GetCurrentGame } from "./common/gameCore/gameRepository";
import { CurrentGameContext, CurrentUserAccountContext } from "./common/gameCore/gameContext";
import UserAccount from "./common/accounts";
import { Game } from "./common/gameCore/gameModels";

const inter = Inter({ subsets: ["latin"] });

function refreshOnNewVersion() {
  if (!('serviceWorker' in navigator)) {
    return;
  }

  navigator.serviceWorker.ready.then(registration => {
    registration.onupdatefound = () => {
      const installingWorker = registration.installing;
      if (!installingWorker) {
        return;
      }

      installingWorker.onstatechange = () => {
        if (installingWorker.state !== 'installed') {
          return;
        }

        if (!navigator.serviceWorker.controller) {
          console.log('Content is cached for offline use.');
          return;
        }

        // New content is available; refresh the page automatically
        console.log('New content is available; refreshing...');
        window.location.reload();
      };
    };
  }).catch(error => {
    console.log('SW registration failed: ', error);
  });
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const pathname = usePathname();
  
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

  // register autorefresh on new version
  useEffect(() => refreshOnNewVersion());

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
      <head>
        <title>The License Plate Game</title>
        <base href="/" />
        <link rel="icon" type="image/x-icon" href="favicon.ico" />
        {/* Enable PWA */}
        <link rel="manifest" href="manifest.json" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta charSet="utf-8" />
      </head>
      <body className={inter.className}>
        <CurrentUserAccountContext.Provider value={userAccount}>
          <CurrentGameContext.Provider value={{
             currentGame,
             setNewCurrentGame: (newCurrentGame) => {
              setCurrentGame(newCurrentGame);
             }}}>

            <div className="flex-col min-h-screen">
              <header className="flex-row rounded-lg bg-blue-500 p-4">
                <nav className="flex items-center">
                  <div className="flex-none w-1/3">
                    <h1 className="text-2xl">License Plate Game</h1>
                  </div>
                  <div className="flex grow gap-5 justify-end text-lg">
                    <Link href="/game" className={`link ${pathname === '/game' ? 'font-extrabold' : ''}`}>Game</Link>
                    <Link href="/history" className={`link ${pathname === '/history' ? 'font-extrabold' : ''}`}>History</Link>
                    <Link href="/about" className={`link ${pathname === '/about' ? 'font-extrabold' : ''}`}>About</Link>
                    <div>
                      <a className="text white p-4">Share App</a>
                    </div>
                  </div>
                </nav>
              </header>

              <main className="flex flex-col">
                <div className="mt-4">
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

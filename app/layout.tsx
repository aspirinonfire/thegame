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
import Image from 'next/image'
import { Drawer } from "flowbite-react";

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

  // is drawer menu open
  const [isDrawerMenuOpen, setIsDrawerMenuOpen] = useState(false);

  // fetch user account, and game data if not tracked
  useEffect(() => {
    async function FetchData() {
      if (!needsFetch) {
        return;
      }
      const [fetchedUserAccount, fetchedCurrentGame] = await Promise.all([
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

  function renderNavLinks() {
    return (
      <>
        <Link href="/game" className={`link ${pathname === '/game' ? 'font-extrabold' : ''}`}>Game</Link>
        <Link href="/history" className={`link ${pathname === '/history' ? 'font-extrabold' : ''}`}>History</Link>
        <Link href="/about" className={`link ${pathname === '/about' ? 'font-extrabold' : ''}`}>About</Link>
        <a href="#" className="block">Share App</a>
      </>
    );
  }

  function renderNavBar() {
    return (
      <nav className="flex items-center justify-between">
        <div className="flex flex-row items-center gap-3">
          <Image
            src="/icons/license-plate-outlined-50.png"
            alt="Game Logo"
            width={50}
            height={50}
          />
          <span className="text-2xl text-gray-100">License Plate Game</span>
        </div>
        <button type="button"
          className="inline-flex items-center p-2 w-10 h-10 justify-center md:hidden text-sm hover:bg-gray-700 focus:ring-gray-600" aria-controls="navbar-default"
          onClick={() => setIsDrawerMenuOpen(true)}>
          <span className="sr-only">Open main menu</span>
          <svg className="w-5 h-5" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 17 14">
            <path stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M1 1h15M1 7h15M1 13h15" />
          </svg>
        </button>
        <div className="hidden md:flex grow gap-5 justify-end text-lg">
          {renderNavLinks()}
        </div>
        <Drawer className="flex flex-col gap-10 md:hidden bg-gray-700"
          open={isDrawerMenuOpen}
          onClose={() => setIsDrawerMenuOpen(false)}
          position="right"
          backdrop={true}>
          <h2 className="text-lg text-gray-300">Where to?</h2>
          <hr />
          <Drawer.Items className="pl-3">
            <div className="flex h-full flex-col justify-between gap-6 text-2xl" onClick={() => setIsDrawerMenuOpen(false)}>
              {renderNavLinks()}
            </div>
          </Drawer.Items>
        </Drawer>
      </nav> );
  }

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
            }
          }}>

            <div className="flex flex-col min-h-screen">
              <header className="flex-row rounded-lg bg-gradient-to-r from-gray-700 to-gray-900 p-4">
                { renderNavBar() }
              </header>

              <main className={`flex flex-col flex-grow mt-4 rounded-lg bg-gradient-to-bl from-10% from-gray-700 to-gray-900 text-gray-300 p-4 transition-all ${isDrawerMenuOpen ? 'blur-sm' : ''}`}>
                <div className={ needsFetch ? "animate-pulse": ""}>
                  {needsFetch ? (<p>Fetching Game data...</p>) : (<>{children}</>)}
                </div>
              </main>
            </div>
          </CurrentGameContext.Provider>
        </CurrentUserAccountContext.Provider>
      </body>
    </html> );
  }

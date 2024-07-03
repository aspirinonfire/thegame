"use client"
import { Inter } from "next/font/google";
import "./globals.css";
import Link from 'next/link';
import { usePathname } from 'next/navigation'
import { useEffect, useState } from "react";
import { GetAccount, GetCurrentGame } from "./common/gameCore/gameRepository";
import { CurrentGameContext, CurrentUserAccountContext, GameContext } from "./common/gameCore/gameContext";
import UserAccount from "./common/accounts";
import Image from 'next/image'
import { Drawer, Spinner } from "flowbite-react";
import { HiOutlineMap, HiOutlineClock, HiOutlineInformationCircle, HiOutlineArrowCircleRight } from "react-icons/hi";
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
  const [activeGame, setActiveGame] = useState<Game | null>(null);

  // track is fetching
  const [needsFetch, setNeedsFetch] = useState(true);

  // is drawer menu open
  const [isDrawerMenuOpen, setIsDrawerMenuOpen] = useState(false);

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
  });

  // register autorefresh on new version
  useEffect(() => refreshOnNewVersion());

  const gameContext = { activeGame, setActiveGame } as GameContext;

  function renderNavLinks() {
    return (
      <>
        <div className={`flex flex-row gap-1 items-center ${pathname === '/game/' ? 'underline' : 'opacity-80'}`}>
          <HiOutlineMap />
          <Link href="/game" replace={true} className="link">Game</Link>
        </div>

        <div className={`flex flex-row gap-1 items-center ${pathname === '/history/' ? 'underline' : 'opacity-80'}`}>
          <HiOutlineClock />
          <Link href="/history" replace={true} className="link">History</Link>
        </div>

        <div className={`flex flex-row gap-1 items-center ${pathname === '/about/' ? 'underline' : 'opacity-80'}`}>
          <HiOutlineInformationCircle />
          <Link href="/about" replace={true} className="link">About</Link>
        </div>
      </>
    );
  }

  function renderNavBar() {
    return (
      <nav>
        <div className="flex flex-row items-center justify-between">
          <div className="flex flex-row items-center gap-3">
            <Image
              src="/icons/license-plate-outlined-50.png"
              alt="Game Logo"
              width={50}
              height={50}
              style={{ filter: "none !important"}}
            />
            <span className="text-lg text-gray-100 uppercase">License Plate Game</span>
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
          <Drawer className="flex flex-col gap-10 md:hidden bg-gradient-to-t from-gray-800 to-gray-900 drop-shadow-lg"
            open={isDrawerMenuOpen}
            onClose={() => setIsDrawerMenuOpen(false)}
            position="top"
            backdrop={true}>
            
            <div className="flex flex-row justify-start items-center gap-3">
              <HiOutlineArrowCircleRight className="w-7 h-7" />
              <h2 className="text-lg text-gray-300">Where to?</h2>
            </div>

            <hr />
            <Drawer.Items className="pl-3">
              <div className="flex h-full flex-col sm:flex-row grow justify-center gap-8 text-lg pb-5" onClick={() => setIsDrawerMenuOpen(false)}>
                {renderNavLinks()}
              </div>
            </Drawer.Items>
          </Drawer>
        </div>
        
        {/* TODO pretty UI */}
        {!!userAccount ?
          (
            <>
              <p className="text-sm text-gray-500 text-wrap">Hello {userAccount?.playerName}</p>
            </>
          ): null}
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
        <meta name="color-scheme" content="light only" />
      </head>
      <body className={inter.className}>
        <CurrentUserAccountContext.Provider value={userAccount}>
          <CurrentGameContext.Provider value={gameContext}>
            <div className="flex flex-col min-h-screen">
              <header className="flex-row rounded-lg bg-gradient-to-r from-gray-900 to-slate-700 p-4">
                { renderNavBar() }
              </header>

              <main className={`flex flex-col flex-grow mt-4 rounded-lg bg-gradient-to-bl from-10% from-slate-700 to-gray-900 text-gray-300 p-4 transition-all ${isDrawerMenuOpen ? 'blur-sm' : ''}`}>
                { needsFetch ?
                  (<div className="flex items-center justify-center m-auto"><Spinner color="info" className="h-20 w-20"/></div>) :
                  (<>{children}</>)}
              </main>
            </div>
          </CurrentGameContext.Provider>
        </CurrentUserAccountContext.Provider>
      </body>
    </html> );
  }

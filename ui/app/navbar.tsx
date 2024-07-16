import Link from 'next/link';
import Image from 'next/image'
import { Drawer, Dropdown } from 'flowbite-react';
import { HiOutlineMap, HiOutlineClock, HiOutlineInformationCircle, HiOutlineArrowCircleRight, HiUserCircle } from "react-icons/hi";
import { usePathname } from 'next/navigation';
import GoogleSignIn from './googleLogin';
import { useContext, useState } from 'react';
import { CurrentUserAccountContext } from './common/gameCore/gameContext';

interface GameNavBarArgs {
  isAuthenticated: boolean
  isDrawerMenuOpen: boolean,
  setIsDrawerMenuOpen: (isDrawerMenuOpen: boolean) => void,
  setNeedsDataRefetch: (needsRefetch: boolean) => void
}

export default function GameNavBar({ isAuthenticated, isDrawerMenuOpen, setIsDrawerMenuOpen, setNeedsDataRefetch } : GameNavBarArgs) {
  const pathname = usePathname();
  const userAccount = useContext(CurrentUserAccountContext);

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

  return (
    <nav>
      <div className="flex flex-row items-center justify-between">
        <div className="flex flex-row items-center gap-3">
          <Image
            src="/icons/license-plate-outlined-50.png"
            alt="Game Logo"
            width={50}
            height={50}
            style={{ filter: "none !important" }}
          />
          <span className="text-lg text-gray-100 uppercase">License Plate Game</span>
        </div>

        <div className="flex flex-row justify-end gap-1 md:gap-3 md:flex-row-reverse">
          <Dropdown
            className="bg-gray-800 drop-shadow-lg text-gray-400 w-1/2 md:w-1/3"
            arrowIcon={false}
            inline
            label={
              <HiUserCircle className="w-8 h-8" />
            }>

            <Dropdown.Header>
              <span className="block text-sm text-gray-200">Hello, {userAccount?.playerName ?? "Guest"}</span>
            </Dropdown.Header>
            <Dropdown.Item className="text-gray-400">Players</Dropdown.Item>
            <Dropdown.Item className="text-gray-400">
              {isAuthenticated ?
                (<>
                  Sign out
                </>) :
                (<>
                  <GoogleSignIn raiseSignedInEvent={() => setNeedsDataRefetch(true)} />
                </>)}
            </Dropdown.Item>
          </Dropdown>

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
      </div>
    </nav>);
}
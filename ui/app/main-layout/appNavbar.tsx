import { MapIcon, ClockIcon, InformationCircleIcon, UserCircleIcon } from "@heroicons/react/24/outline";
import { Dropdown, DropdownHeader, DropdownItem, Navbar, NavbarBrand, NavbarCollapse, NavbarLink, NavbarToggle, Spinner } from "flowbite-react";
import type { ElementType } from "react";
import { Link, useLocation, useNavigate } from "react-router";
import { useShallow } from "zustand/shallow";
import { useAppState } from "~/appState/useAppState";

const AppNavbar = () => {
  const pathname = useLocation();
  const navigate = useNavigate();

  const [
    activeUser,
    isGsiSdkReady,
    authenticateWithGoogle,
    isProcessingLogin,
    signOut
  ] = useAppState(useShallow(state => [
    state.activeUser,
    state.isGsiSdkReady,
    state.authenticateWithGoogle,
    state.isProcessingLogin,
    state.signOut]));

  const navlink = (url: string, linkText: string, Icon: ElementType, exactMatch: boolean = false) => {
    const isActive = exactMatch ?
      pathname.pathname == url :
      pathname.pathname.startsWith(url);
    
    return <NavbarLink
      as="div"
      active={isActive}
      className="mb-5 md:m-0 !bg-transparent !border-0" >
        <Link to={url}>
          <span className={`flex flex-row gap-2 items-center text-gray-300 hover:text-gray-500 ${isActive ? 'underline underline-offset-12 decoration-3' : 'opacity-80'}`}>
            <Icon className="h-6" />
            {linkText}
          </span>
        </Link>
    </NavbarLink>
  }

  const renderAppSignIn = () => {
    return <>
      <DropdownItem className="text-gray-400" onClick={authenticateWithGoogle} disabled={ isProcessingLogin || !isGsiSdkReady }>
        {isProcessingLogin ?
          <Spinner size="sm" color="alternative" /> :
          isGsiSdkReady ? "Sign-in with Google" : "Setting up Google Sign-in..."}
      </DropdownItem>
    </>
  }

  const renderAppSignOut = () => {
    return <>
      <DropdownItem className="text-gray-400" onClick={signOut} disabled={ isProcessingLogin }>
        {isProcessingLogin ?
          <Spinner size="sm" color="alternative" /> :
          "Sign-out"
        }
      </DropdownItem>
    </>
  }

  return <Navbar fluid className="gap-5 bg-gradient-to-t from-gray-800 to-gray-900 drop-shadow-lg pl-5 py-0">
    <NavbarBrand as={Link} href="/">
      <img src="/icons/license-plate-outlined-100.png" alt="Game Logo" className="mr-3 h-15" />
      <span className="text-lg text-gray-100 uppercase">License Plate Game</span>
    </NavbarBrand>

    <div className="flex flex-row justify-end gap-2 md:gap-0 md:flex-row-reverse">
      <Dropdown
        className="bg-gray-800 drop-shadow-lg text-gray-400 w-1/2 md:w-1/3"
        arrowIcon={false}
        inline
        label={
          <UserCircleIcon className="w-8 h-8 text-gray-400" />
        }>

        <DropdownHeader>
          <span className="block text-sm text-gray-200">Hello, {activeUser?.player.playerName}</span>
        </DropdownHeader>

        { activeUser.isAuthenticated ?
          renderAppSignOut() :
          renderAppSignIn() }
      
      </Dropdown>

      <NavbarToggle className="text-gray-400" />
    </div>

    <NavbarCollapse>
      <div className="block md:flex md:flex-row md:gap-5 m-0 p-0">
        {navlink('/game', 'Game', MapIcon)}
        {navlink('/history', 'History', ClockIcon)}
        {navlink('/about', 'About', InformationCircleIcon)}
      </div>
    </NavbarCollapse>
  </Navbar>
}

export default AppNavbar;
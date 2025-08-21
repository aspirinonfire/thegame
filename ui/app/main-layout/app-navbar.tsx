import { MapIcon, ClockIcon, InformationCircleIcon } from "@heroicons/react/24/outline";
import { Navbar, NavbarBrand, NavbarCollapse, NavbarLink, NavbarToggle } from "flowbite-react";
import type { ElementType } from "react";
import { Link, useLocation } from "react-router";

const AppNavbar = () => {
  const pathname = useLocation();

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

  return <Navbar fluid className="gap-5 bg-gradient-to-t from-gray-800 to-gray-900 drop-shadow-lg pl-5 py-0">
    <NavbarBrand as={Link} href="/">
      <img src="/icons/license-plate-outlined-100.png" alt="Game Logo" className="mr-3 h-15" />
      <span className="text-lg text-gray-100 uppercase">License Plate Game</span>
    </NavbarBrand>

    <NavbarToggle className="text-gray-400" />

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
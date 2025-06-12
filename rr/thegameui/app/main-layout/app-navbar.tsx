import { Navbar, NavbarCollapse, NavbarLink, NavbarToggle } from "flowbite-react";
import { Link, useLocation } from "react-router";

const AppNavbar = () => {
  const pathname = useLocation();

  const navlink = (url: string, linkText: string, exactMatch: boolean = false) => {
    const isActive = exactMatch ?
      pathname.pathname == url :
      pathname.pathname.startsWith(url);
    
    return <NavbarLink
      as="div"
      active={isActive}
      className="bg-white mb-1 md:mb-0" >
        <Link to={url}>
          <span className={`text-gray-700 hover:text-gray-500 ${isActive ? 'underline underline-offset-12 decoration-3' : 'opacity-80'}`}>
            {linkText}
          </span>
        </Link>
    </NavbarLink>
  }

  return <Navbar fluid>
    <NavbarToggle />

    <NavbarCollapse className="md:w-full [&>ul]:md:justify-between">
      <div className="block md:flex md:flex-row md:gap-5">
        {navlink('/', 'Home', true)}
        {navlink('/about', 'About')}
      </div>
    </NavbarCollapse>
  </Navbar>
}

export default AppNavbar;
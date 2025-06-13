import { Outlet } from "react-router";

const DefaultLayout = () => {
  return (
    <div className="flex flex-col gap-5 justify-start p-5 min-h-[calc(100vh-20rem)] min-w-[calc(100dvw-5dvw)]">
      <Outlet />
    </div>
  );
}

export default DefaultLayout;
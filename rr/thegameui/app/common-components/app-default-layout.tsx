import { Outlet } from "react-router";

const DefaultLayout = () => {
  return (
    <div className="flex flex-col gap-5 justify-start p-5 min-h-[calc(100vh-9rem)] min-w-[calc(100dvw-1dvw)]">
      <Outlet /> 
    </div>
  );
}

export default DefaultLayout;
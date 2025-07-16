import {
  isRouteErrorResponse,
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
  type LinkDescriptor,
} from "react-router";

import type { Route } from "./+types/root";
import "./app.css";
import { Button, ThemeConfig } from "flowbite-react";
import AppNavbar from "./main-layout/appNavbar";
import { useAppState } from "./appState/useAppState";
import { useShallow } from "zustand/shallow";
import { useEffect } from "react";
import LoadingWidget from "./common-components/loading";

// force sw registration
import './register-sw.client';
import ErrorBoundary from "./main-layout/appErrorBoundary";
import AppMessages from "./main-layout/appMessages";


export const links: Route.LinksFunction = (): LinkDescriptor[] => {
  const links: LinkDescriptor[] = [
    { rel: "preconnect", href: "https://fonts.googleapis.com" },
    { rel: "preconnect", href: "https://fonts.gstatic.com", crossOrigin: "anonymous" },
    {
      rel: "stylesheet",
      href: "https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap",
    },
  ];

  // PWA manifest only in production
  if (import.meta.env.PROD) {
    links.push({ rel: "manifest", href: "/manifest.webmanifest" });
  }

  return links;
};

export function Layout({ children }: { children: React.ReactNode }) {
  const [hasInitialized, initialize] = useAppState(useShallow((state) => 
    [state.isInitialized, state.initialize]));

  useEffect(() => {
    if (hasInitialized) {
      return;
    }

    initialize();
  }, [hasInitialized, initialize]);

  return (
    <html lang="en">
      <head>
        <title>The License Plate Game</title>
        <base href="/" />
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <Meta />
        <Links />
      </head>
      <body className="antialiased min-h-screen flex flex-col bg-gray-300">
        <ThemeConfig dark={false} />
        {/* Error boundary for the entire app */}
        <ErrorBoundary>
          <header className="bg-gray-300">
            <div className="p-0">
              <AppNavbar />
            </div>
          </header>
  
          <main>
            <AppMessages/>
            {/* Error boundary for a selected view */}
            <ErrorBoundary>
              { hasInitialized ? children : <LoadingWidget/> }
            </ErrorBoundary>
          </main>
        </ErrorBoundary>

        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  );
}

export default function App() {
  return <Outlet />;
}

export function HydrateFallback() {
  return <LoadingWidget/>
}

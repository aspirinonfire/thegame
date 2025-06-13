import {
  isRouteErrorResponse,
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
} from "react-router";

import type { Route } from "./+types/root";
import "./app.css";
import { Button, ThemeConfig } from "flowbite-react";
import AppNavbar from "./main-layout/app-navbar";
import { useAppStore } from "./useAppStore";
import { useShallow } from "zustand/shallow";
import { useEffect } from "react";
import LoadingWidget from "./common-components/loading";

export const links: Route.LinksFunction = () => [
  { rel: "preconnect", href: "https://fonts.googleapis.com" },
  {
    rel: "preconnect",
    href: "https://fonts.gstatic.com",
    crossOrigin: "anonymous",
  },
  {
    rel: "stylesheet",
    href: "https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap",
  },
];

export function Layout({ children }: { children: React.ReactNode }) {
  const [hasInitialized, initialize] = useAppStore(useShallow((state) => 
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
        <header className="bg-gray-300">
          <div className="p-0">
            <AppNavbar />
          </div>
        </header>
        { hasInitialized ? children : <LoadingWidget/> }
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

export function ErrorBoundary({ error }: Route.ErrorBoundaryProps) {
  let message = "Oops!";
  let details = "An unexpected error occurred.";
  let stack: string | undefined;

  if (isRouteErrorResponse(error)) {
    message = error.status === 404 ? "404" : "Error";
    details =
      error.status === 404
        ? "The requested page could not be found."
        : error.statusText || details;
  } else if (import.meta.env.DEV && error && error instanceof Error) {
    details = error.message;
    stack = error.stack;
  }

  return (
    <main className="flex flex-col gap-4 items-center justify-center p-5 min-h-[calc(100vh-20rem)] min-w-[calc(100dvw-5dvw)]">
      <h1>{message}</h1>
      <p>{details}</p>
      {stack && (
        <pre className="w-full p-4 overflow-x-auto">
          <code>{stack}</code>
        </pre>
      )}
      <Button size="xs" className="mt-10" onClick={() => window.location.href = '/'}>
        Go back to Main Screen
      </Button>
    </main>
  );
}

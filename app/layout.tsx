import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import Link from 'next/link';

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "License Plate Game",
  description: "License Plate Game by Alex Chernyak",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <div className="flex-col min-h-screen">
          <div className="flex h-20 shrink-0 items-end rounded-lg bg-blue-500 p-4">
            <nav className="inline-flex items-center gap-20">
              <h1 className="text-2xl">License Plate Game</h1>
              <div className="inline-flex items-center gap-4">
                <Link href="/game">Game</Link>
                <Link href="/history">History</Link>
                <Link href="/about">About</Link>
                <a className="text white p-4">Share App</a>
              </div>
            </nav>
          </div>

          <main className="flex flex-row grow">
            <div className="mt-4 grow">
              {children}
            </div>
          </main>
        </div>
      </body>
    </html>
  );
}

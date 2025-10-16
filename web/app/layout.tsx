import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { SessionProvider } from './SessionProvider';
import NextTopLoader from 'nextjs-toploader';

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "CloudOps Control Plane",
  description: "Enterprise developer platform with queue-based orchestration",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <NextTopLoader 
          color="#33b5ff"
          height={3}
          showSpinner={false}
          speed={200}
          shadow="0 0 10px #33b5ff,0 0 5px #33b5ff"
        />
        <SessionProvider>
          {children}
        </SessionProvider>
      </body>
    </html>
  );
}

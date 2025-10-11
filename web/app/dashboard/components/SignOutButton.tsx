'use client';

import { signOut } from 'next-auth/react';

export default function SignOutButton() {
  const handleSignOut = async () => {
    await signOut({ callbackUrl: '/', redirect: true });
  };

  return (
    <button
      onClick={handleSignOut}
      className="px-3 py-2 rounded-md text-sm font-medium transition-colors hover:opacity-80"
      style={{ 
        color: 'var(--foreground)',
        border: '1px solid var(--border)'
      }}
    >
      Sign Out
    </button>
  );
}

'use client';

import { signOut } from 'next-auth/react';
import type { User } from 'next-auth';

interface DashboardNavProps {
  user: User;
}

export default function DashboardNav({ user }: DashboardNavProps) {
  return (
    <nav className="bg-slate-950/80 backdrop-blur border-b border-slate-800">
      <div className="max-w-7xl mx-auto px-8 py-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-white">
              CloudOps Control Plane
            </h1>
          </div>

          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-sm text-slate-400">Signed in as</p>
              <p className="text-white font-medium">{user.name || user.email}</p>
            </div>
            
            <button
              onClick={() => signOut({ callbackUrl: '/' })}
              className="px-4 py-2 bg-cyan-500 hover:bg-cyan-600 text-white rounded-lg transition-all"
            >
              Sign Out
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
}

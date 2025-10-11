'use client';

import { signOut } from 'next-auth/react';
import type { User } from 'next-auth';

interface DashboardNavProps {
  user: User;
}

export default function DashboardNav({ user }: DashboardNavProps) {
  return (
    <nav className="bg-slate-900 border-b border-slate-800">
      <div className="max-w-7xl mx-auto px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-8">
            <span className="text-2xl font-bold text-white">optym</span>
            <span className="text-xl text-white">CloudOps</span>
          </div>

          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-sm text-slate-400">Signed in as</p>
              <p className="text-white font-medium">{user.name || user.email}</p>
            </div>
            
            <button
              onClick={() => signOut({ callbackUrl: '/' })}
              className="px-6 py-2 bg-cyan-500 hover:bg-cyan-600 text-white font-medium rounded-lg transition-colors"
            >
              Sign Out
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
}

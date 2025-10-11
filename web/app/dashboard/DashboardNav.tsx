'use client';

import { signOut } from 'next-auth/react';
import type { User } from 'next-auth';

interface DashboardNavProps {
  user: User;
}

export default function DashboardNav({ user }: DashboardNavProps) {
  return (
    <nav style={{ borderBottom: '1px solid var(--border)' }}>
      <div className="max-w-7xl mx-auto px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-8">
            <img src="/optym-logo.png" alt="Optym" className="h-8" />
            <span className="text-xl" style={{ color: 'var(--foreground)' }}>CloudOps</span>
          </div>

          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Signed in as</p>
              <p className="font-medium" style={{ color: 'var(--foreground)' }}>{user.name || user.email}</p>
            </div>
            
            <button
              onClick={() => signOut({ callbackUrl: '/' })}
              className="px-6 py-2 font-medium rounded-lg transition-colors"
              style={{ 
                backgroundColor: 'var(--primary)', 
                color: 'var(--primary-foreground)' 
              }}
            >
              Sign Out
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
}

'use client';

import { signOut } from 'next-auth/react';
import type { User } from 'next-auth';
import SubscriptionSelector from './components/SubscriptionSelector';

interface DashboardNavProps {
  user: User;
}

export default function DashboardNav({ user }: DashboardNavProps) {
  return (
    <nav className="relative" style={{ 
      background: 'linear-gradient(180deg, hsl(220, 15%, 12%) 0%, hsl(220, 15%, 6%) 100%)',
      borderBottom: '1px solid var(--border)' 
    }}>
      {/* Top gradient overlay */}
      <div 
        className="absolute top-0 left-0 right-0 h-32 pointer-events-none"
        style={{
          background: 'linear-gradient(180deg, hsl(175, 70%, 50%, 0.08) 0%, transparent 100%)',
          zIndex: 0
        }}
      />
      
      <div className="max-w-7xl mx-auto px-6 py-4 relative z-10">
        <div className="flex items-center justify-end gap-4">
          <SubscriptionSelector />
          
          <div className="flex items-center gap-3" style={{ borderLeft: '1px solid var(--border)', paddingLeft: '1rem' }}>
            <div className="text-right">
              <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
                {user.name || user.email}
              </p>
            </div>
            
            <button
              onClick={() => signOut({ callbackUrl: '/' })}
              className="px-4 py-2 text-sm font-medium rounded-lg transition-colors"
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

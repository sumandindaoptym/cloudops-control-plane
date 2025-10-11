'use client';

import { signOut } from 'next-auth/react';
import type { User } from 'next-auth';
import SubscriptionSelector from './components/SubscriptionSelector';

interface DashboardNavProps {
  user: User;
}

export default function DashboardNav({ user }: DashboardNavProps) {
  return (
    <nav style={{ 
      backgroundColor: 'var(--card)',
      borderBottom: '1px solid var(--border)' 
    }}>
      <div className="max-w-7xl mx-auto px-6 py-4">
        <div className="flex items-center justify-end gap-4">
          <SubscriptionSelector />
        </div>
      </div>
    </nav>
  );
}

'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

interface NavItem {
  icon: string;
  label: string;
  href: string;
  badge?: string;
}

const navItems: NavItem[] = [
  { icon: '📊', label: 'Overview', href: '/dashboard' },
  { icon: '🚀', label: 'Deployments', href: '/dashboard/deployments' },
  { icon: '💾', label: 'Databases', href: '/dashboard/databases' },
  { icon: '🔄', label: 'Tasks', href: '/dashboard/tasks', badge: '3' },
  { icon: '📦', label: 'Projects', href: '/dashboard/projects' },
  { icon: '🌍', label: 'Environments', href: '/dashboard/environments' },
  { icon: '⚙️', label: 'Settings', href: '/dashboard/settings' },
];

export default function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="w-64 h-screen sticky top-0" style={{ 
      backgroundColor: 'var(--card)', 
      borderRight: '1px solid var(--border)' 
    }}>
      <div className="p-6">
        <nav className="space-y-2">
          {navItems.map((item) => {
            const isActive = pathname === item.href;
            
            return (
              <Link
                key={item.href}
                href={item.href}
                className="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors"
                style={
                  isActive
                    ? { backgroundColor: 'var(--primary)', color: 'var(--primary-foreground)' }
                    : { color: 'var(--muted-foreground)' }
                }
              >
                <span className="text-xl">{item.icon}</span>
                <span className="font-medium">{item.label}</span>
                {item.badge && (
                  <span className="ml-auto text-xs font-bold px-2 py-0.5 rounded-full" style={{ 
                    backgroundColor: 'var(--primary-bg)', 
                    color: 'var(--primary)',
                    border: '1px solid var(--primary)'
                  }}>
                    {item.badge}
                  </span>
                )}
              </Link>
            );
          })}
        </nav>

        <div className="mt-8 pt-6" style={{ borderTop: '1px solid var(--border)' }}>
          <div className="flex items-center gap-2 text-sm" style={{ color: 'var(--muted-foreground)' }}>
            <div className="w-2 h-2 rounded-full animate-pulse" style={{ backgroundColor: 'var(--success)' }} />
            <span>API Status: Healthy</span>
          </div>
        </div>
      </div>
    </aside>
  );
}

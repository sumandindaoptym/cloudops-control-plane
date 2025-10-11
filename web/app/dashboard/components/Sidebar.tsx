'use client';

import { useState } from 'react';
import Link from 'next/link';

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
  const [activeItem, setActiveItem] = useState('/dashboard');

  return (
    <aside className="w-64 h-screen sticky top-0 bg-slate-800/50 border-r border-slate-700">
      <div className="p-6">
        <nav className="space-y-2">
          {navItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              onClick={() => setActiveItem(item.href)}
              className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                activeItem === item.href
                  ? 'bg-cyan-500 text-white'
                  : 'text-slate-400 hover:text-white hover:bg-slate-700/50'
              }`}
            >
              <span className="text-xl">{item.icon}</span>
              <span className="font-medium">{item.label}</span>
              {item.badge && (
                <span className="ml-auto bg-cyan-500 text-white text-xs font-bold px-2 py-0.5 rounded-full">
                  {item.badge}
                </span>
              )}
            </Link>
          ))}
        </nav>

        <div className="mt-8 pt-6 border-t border-slate-700">
          <div className="flex items-center gap-2 text-slate-400 text-sm">
            <div className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" />
            <span>API Status: Healthy</span>
          </div>
        </div>
      </div>
    </aside>
  );
}

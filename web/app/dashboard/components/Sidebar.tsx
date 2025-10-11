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
    <aside className="w-64 h-screen sticky top-0 p-4">
      <div className="relative h-full">
        <div className="absolute inset-0 bg-gradient-to-br from-cyan-500/10 to-blue-500/10 rounded-3xl blur-xl" />
        <div className="relative h-full bg-slate-900/40 backdrop-blur-xl border border-white/10 rounded-3xl p-6 flex flex-col">
          <div className="mb-8">
            <div className="flex items-center gap-3 mb-2">
              <div className="w-10 h-10 bg-gradient-to-br from-cyan-500 to-blue-500 rounded-xl flex items-center justify-center text-xl">
                ☁️
              </div>
              <div>
                <h2 className="text-white font-bold text-lg">CloudOps</h2>
                <p className="text-slate-400 text-xs">Control Plane</p>
              </div>
            </div>
          </div>

          <nav className="flex-1 space-y-2">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => setActiveItem(item.href)}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl transition-all duration-200 ${
                  activeItem === item.href
                    ? 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30'
                    : 'text-slate-400 hover:text-white hover:bg-white/5'
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

          <div className="mt-auto pt-6 border-t border-white/10">
            <div className="flex items-center gap-3 text-slate-400 text-sm">
              <div className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" />
              <span>API Status: Healthy</span>
            </div>
          </div>
        </div>
      </div>
    </aside>
  );
}

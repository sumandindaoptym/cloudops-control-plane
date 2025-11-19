'use client';

import { signIn } from 'next-auth/react';

export default function LandingPage() {
  return (
    <div className="min-h-screen relative" style={{ backgroundColor: 'var(--background)' }}>
      {/* Top gradient overlay */}
      <div 
        className="absolute top-0 left-0 right-0 h-96 pointer-events-none"
        style={{
          background: 'linear-gradient(180deg, hsl(175, 70%, 50%, 0.12) 0%, transparent 60%)',
          zIndex: 0
        }}
      />
      
      {/* Navigation */}
      <nav className="relative z-10" style={{ 
        background: 'linear-gradient(180deg, hsl(220, 15%, 12%) 0%, hsl(220, 15%, 6%) 100%)',
        borderBottom: '1px solid var(--border)' 
      }}>
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-8">
            <div className="flex items-center gap-3">
              <img src="/optym-logo.png" alt="Optym" className="h-8" />
            </div>
            <span className="text-xl" style={{ color: 'var(--foreground)' }}>CloudOps</span>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <div className="max-w-5xl mx-auto px-6 pt-24 pb-16 text-center relative z-10">
        <h1 className="text-6xl font-bold mb-6" style={{ color: 'var(--foreground)' }}>
          CloudOps
        </h1>
        <p className="text-xl mb-10 max-w-3xl mx-auto" style={{ color: 'var(--muted-foreground)' }}>
          Comprehensive organizational management platform for teams and projects. Track
          allocations, manage resources, and drive operational excellence.
        </p>
        <button
          onClick={() => signIn('azure-ad', { callbackUrl: '/dashboard' })}
          className="px-8 py-3 font-medium rounded-lg transition-colors text-lg flex items-center gap-3 mx-auto"
          style={{ 
            backgroundColor: 'var(--primary)', 
            color: 'var(--primary-foreground)' 
          }}
        >
          <svg className="w-6 h-6" viewBox="0 0 23 23" fill="currentColor">
            <path d="M0 0h10.93v10.93H0V0zm12.07 0H23v10.93H12.07V0zM0 12.07h10.93V23H0V12.07zm12.07 0H23V23H12.07V12.07z"/>
          </svg>
          Sign in With Microsoft
        </button>
      </div>

      {/* Features Grid */}
      <div className="max-w-7xl mx-auto px-6 py-16">
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div className="rounded-xl p-8 text-center" style={{ 
            backgroundColor: 'var(--card)', 
            border: '1px solid var(--border)' 
          }}>
            <div className="w-16 h-16 mx-auto mb-6 flex items-center justify-center">
              <svg className="w-12 h-12" fill="none" stroke="var(--primary)" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <h3 className="text-xl font-bold mb-3" style={{ color: 'var(--card-foreground)' }}>One-Click Deploy</h3>
            <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
              Deploy applications with a single click using automated workflows
            </p>
          </div>

          <div className="rounded-xl p-8 text-center" style={{ 
            backgroundColor: 'var(--card)', 
            border: '1px solid var(--border)' 
          }}>
            <div className="w-16 h-16 mx-auto mb-6 flex items-center justify-center">
              <svg className="w-12 h-12" fill="none" stroke="var(--primary)" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4m0 5c0 2.21-3.582 4-8 4s-8-1.79-8-4" />
              </svg>
            </div>
            <h3 className="text-xl font-bold mb-3" style={{ color: 'var(--card-foreground)' }}>Database Management</h3>
            <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
              Automated database backups and restores with one-click operations across all environments
            </p>
          </div>

          <div className="rounded-xl p-8 text-center" style={{ 
            backgroundColor: 'var(--card)', 
            border: '1px solid var(--border)' 
          }}>
            <div className="w-16 h-16 mx-auto mb-6 flex items-center justify-center">
              <svg className="w-12 h-12" fill="none" stroke="var(--primary)" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
              </svg>
            </div>
            <h3 className="text-xl font-bold mb-3" style={{ color: 'var(--card-foreground)' }}>Real-time Analytics</h3>
            <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
              Get insights into resource utilization with comprehensive dashboards and reports
            </p>
          </div>

          <div className="rounded-xl p-8 text-center" style={{ 
            backgroundColor: 'var(--card)', 
            border: '1px solid var(--border)' 
          }}>
            <div className="w-16 h-16 mx-auto mb-6 flex items-center justify-center">
              <svg className="w-12 h-12" fill="none" stroke="var(--primary)" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <h3 className="text-xl font-bold mb-3" style={{ color: 'var(--card-foreground)' }}>Secure Access</h3>
            <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
              Role-based permissions ensure data security with admin, manager, and employee access levels
            </p>
          </div>
        </div>
      </div>

      {/* CTA Section */}
      <div className="max-w-7xl mx-auto px-6 py-20">
        <div className="rounded-2xl p-12 text-center" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <p className="mb-8 text-2xl font-semibold" style={{ color: 'var(--card-foreground)' }}>
            Ease your daily devops tasks with CloudOps platform tool.
          </p>
          <button
            onClick={() => signIn('azure-ad', { callbackUrl: '/dashboard' })}
            className="px-8 py-3 font-medium rounded-lg transition-colors text-lg"
            style={{ 
              backgroundColor: 'var(--primary)', 
              color: 'var(--primary-foreground)' 
            }}
          >
            Start Managing Resources
          </button>
        </div>
      </div>
    </div>
  );
}

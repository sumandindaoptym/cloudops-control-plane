'use client';

import { signIn } from 'next-auth/react';

export default function LandingPage() {
  return (
    <div className="min-h-screen" style={{ backgroundColor: 'var(--background)' }}>
      {/* Navigation */}
      <nav style={{ borderBottom: '1px solid var(--border)' }}>
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-8">
            <div className="flex items-center gap-3">
              <img src="/optym-logo.png" alt="Optym" className="h-8" />
            </div>
            <span className="text-xl" style={{ color: 'var(--foreground)' }}>CloudOps</span>
          </div>
          <button
            onClick={() => signIn('azure-ad', { callbackUrl: '/dashboard' })}
            className="px-6 py-2 font-medium rounded-lg transition-colors"
            style={{ 
              backgroundColor: 'var(--primary)', 
              color: 'var(--primary-foreground)' 
            }}
          >
            Sign In
          </button>
        </div>
      </nav>

      {/* Hero Section */}
      <div className="max-w-5xl mx-auto px-6 pt-24 pb-16 text-center">
        <h1 className="text-6xl font-bold mb-6" style={{ color: 'var(--foreground)' }}>
          CloudOps
        </h1>
        <p className="text-xl mb-10 max-w-3xl mx-auto" style={{ color: 'var(--muted-foreground)' }}>
          Comprehensive organizational management platform for teams and projects. Track
          allocations, manage resources, and drive operational excellence.
        </p>
        <button
          onClick={() => signIn('azure-ad', { callbackUrl: '/dashboard' })}
          className="px-8 py-3 font-medium rounded-lg transition-colors text-lg"
          style={{ 
            backgroundColor: 'var(--primary)', 
            color: 'var(--primary-foreground)' 
          }}
        >
          Get Started
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
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <h3 className="text-xl font-bold mb-3" style={{ color: 'var(--card-foreground)' }}>Task Tracking</h3>
            <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
              Track time allocation month-over-month with automatic validation to prevent over-allocation
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
          <h2 className="text-3xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>
            Ready to optimize your team's productivity?
          </h2>
          <p className="mb-8 text-lg" style={{ color: 'var(--muted-foreground)' }}>
            Join hundreds of companies using CloudOps to manage their organizations efficiently.
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

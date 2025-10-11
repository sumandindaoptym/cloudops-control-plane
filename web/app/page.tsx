'use client';

import { signIn } from 'next-auth/react';

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-blue-950 to-slate-950 flex items-center justify-center p-8">
      <div className="max-w-4xl w-full">
        {/* Hero Section */}
        <div className="text-center mb-12">
          <h1 className="text-6xl font-bold mb-6 text-white">
            CloudOps Control Plane
          </h1>
          <p className="text-2xl text-slate-300 mb-4">
            Enterprise Developer Platform
          </p>
          <p className="text-lg text-slate-400 max-w-2xl mx-auto">
            Streamline your cloud operations with queue-based task orchestration,
            real-time updates, and one-click deployments.
          </p>
        </div>

        {/* Features Grid */}
        <div className="grid md:grid-cols-3 gap-6 mb-12">
          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6">
            <div className="text-3xl mb-3">🚀</div>
            <h3 className="text-lg font-semibold text-white mb-2">One-Click Deploy</h3>
            <p className="text-slate-400 text-sm">
              Deploy applications with a single click using automated workflows
            </p>
          </div>

          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6">
            <div className="text-3xl mb-3">📊</div>
            <h3 className="text-lg font-semibold text-white mb-2">Real-Time Updates</h3>
            <p className="text-slate-400 text-sm">
              Monitor task progress with live updates via SignalR
            </p>
          </div>

          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6">
            <div className="text-3xl mb-3">🔄</div>
            <h3 className="text-lg font-semibold text-white mb-2">Task Orchestration</h3>
            <p className="text-slate-400 text-sm">
              FIFO queue-based processing ensures reliable execution
            </p>
          </div>
        </div>

        {/* Login Section */}
        <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-8 text-center">
          <h2 className="text-2xl font-bold text-white mb-4">
            Access Your Dashboard
          </h2>
          <p className="text-slate-400 mb-6">
            Sign in with your Azure AD account to manage your cloud operations
          </p>
          
          <button
            onClick={() => signIn('azure-ad', { callbackUrl: '/dashboard' })}
            className="px-8 py-4 bg-cyan-500 hover:bg-cyan-600 text-white font-semibold rounded-lg transition-all transform hover:scale-105 inline-flex items-center gap-3"
          >
            <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
              <path d="M11.4 24H0V12.6h11.4V24zM24 24H12.6V12.6H24V24zM11.4 11.4H0V0h11.4v11.4zm12.6 0H12.6V0H24v11.4z"/>
            </svg>
            Sign in with Microsoft
          </button>

          <p className="text-slate-500 text-sm mt-6">
            Secure enterprise authentication powered by Azure AD
          </p>
        </div>

        {/* Footer */}
        <div className="mt-12 text-center text-slate-500 text-sm">
          <p>CloudOps Control Plane v1.0 | Optym Enterprise Platform</p>
        </div>
      </div>
    </div>
  );
}

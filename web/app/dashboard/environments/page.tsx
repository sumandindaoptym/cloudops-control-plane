'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

export default function EnvironmentsPage() {
  const [projects, setProjects] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiFetch('/projects')
      .then((data) => {
        setProjects(data);
        setLoading(false);
      })
      .catch((error) => {
        console.error('Error fetching projects:', error);
        setLoading(false);
      });
  }, []);

  const allEnvironments = projects.flatMap(p => 
    (p.environments || []).map((env: any) => ({
      ...env,
      projectName: p.name
    }))
  );

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--foreground)' }}>Environments</h1>
          <p style={{ color: 'var(--muted-foreground)' }}>Manage deployment environments across all projects</p>
        </div>
        <button
          className="px-6 py-3 rounded-lg font-medium transition-colors hover:opacity-90"
          style={{ 
            backgroundColor: 'var(--primary)', 
            color: 'var(--primary-foreground)' 
          }}
        >
          🌍 New Environment
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Total Environments</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--foreground)' }}>
            {allEnvironments.length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Production</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--destructive)' }}>
            {allEnvironments.filter(e => e.type === 'Production').length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Staging</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--primary)' }}>
            {allEnvironments.filter(e => e.type === 'Staging').length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Sandbox</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--success)' }}>
            {allEnvironments.filter(e => e.type === 'Sandbox').length}
          </div>
        </div>
      </div>

      <div className="rounded-xl p-6" style={{ 
        backgroundColor: 'var(--card)', 
        border: '1px solid var(--border)' 
      }}>
        <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>All Environments</h2>
        
        {loading ? (
          <p style={{ color: 'var(--muted-foreground)' }}>Loading environments...</p>
        ) : allEnvironments.length === 0 ? (
          <p style={{ color: 'var(--muted-foreground)' }}>No environments yet. Click "New Environment" to get started.</p>
        ) : (
          <div className="space-y-3">
            {allEnvironments.map((env) => (
              <div 
                key={env.id} 
                className="p-4 rounded-lg transition-colors hover:opacity-90"
                style={{ 
                  backgroundColor: 'var(--secondary)', 
                  border: '1px solid var(--border)' 
                }}
              >
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3">
                      <h3 className="font-semibold" style={{ color: 'var(--card-foreground)' }}>
                        {env.name}
                      </h3>
                      <span 
                        className="px-2 py-1 text-xs font-semibold rounded"
                        style={{
                          backgroundColor: env.type === 'Production' ? 'var(--destructive-bg)' : 
                                          env.type === 'Staging' ? 'var(--primary-bg)' : 'var(--success-bg)',
                          color: env.type === 'Production' ? 'var(--destructive)' : 
                                env.type === 'Staging' ? 'var(--primary)' : 'var(--success)',
                          border: `1px solid ${env.type === 'Production' ? 'var(--destructive)' : 
                                               env.type === 'Staging' ? 'var(--primary)' : 'var(--success)'}`
                        }}
                      >
                        {env.type}
                      </span>
                    </div>
                    <div className="flex items-center gap-6 mt-2 text-sm" style={{ color: 'var(--muted-foreground)' }}>
                      <span>Project: {env.projectName}</span>
                      {env.ttlMinutes && <span>TTL: {env.ttlMinutes} min</span>}
                      {env.expiresAt && <span>Expires: {new Date(env.expiresAt).toLocaleString()}</span>}
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <button
                      className="px-4 py-2 rounded-lg text-sm font-medium transition-colors hover:opacity-90"
                      style={{ 
                        backgroundColor: 'var(--primary)', 
                        color: 'var(--primary-foreground)' 
                      }}
                    >
                      Deploy
                    </button>
                    <button
                      className="px-4 py-2 rounded-lg text-sm font-medium transition-colors hover:opacity-90"
                      style={{ 
                        border: '1px solid var(--border)',
                        color: 'var(--foreground)'
                      }}
                    >
                      Configure
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

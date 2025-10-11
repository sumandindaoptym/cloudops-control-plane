'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

export default function ProjectsPage() {
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

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--foreground)' }}>Projects</h1>
          <p style={{ color: 'var(--muted-foreground)' }}>Manage your cloud projects and configurations</p>
        </div>
        <button
          className="px-6 py-3 rounded-lg font-medium transition-colors hover:opacity-90"
          style={{ 
            backgroundColor: 'var(--primary)', 
            color: 'var(--primary-foreground)' 
          }}
        >
          📦 New Project
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Total Projects</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--foreground)' }}>
            {projects.length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Active Projects</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--success)' }}>
            {projects.length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Total Environments</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--foreground)' }}>
            {projects.reduce((sum, p) => sum + (p.environments?.length || 0), 0)}
          </div>
        </div>
      </div>

      <div className="rounded-xl p-6" style={{ 
        backgroundColor: 'var(--card)', 
        border: '1px solid var(--border)' 
      }}>
        <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>All Projects</h2>
        
        {loading ? (
          <p style={{ color: 'var(--muted-foreground)' }}>Loading projects...</p>
        ) : projects.length === 0 ? (
          <p style={{ color: 'var(--muted-foreground)' }}>No projects yet. Click "New Project" to get started.</p>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {projects.map((project) => (
              <div 
                key={project.id} 
                className="p-6 rounded-lg transition-colors hover:opacity-90 cursor-pointer"
                style={{ 
                  backgroundColor: 'var(--secondary)', 
                  border: '1px solid var(--border)' 
                }}
              >
                <div className="flex items-start justify-between mb-3">
                  <h3 className="text-lg font-semibold" style={{ color: 'var(--card-foreground)' }}>
                    {project.name}
                  </h3>
                  <span 
                    className="px-2 py-1 text-xs font-semibold rounded"
                    style={{
                      backgroundColor: 'var(--primary-bg)',
                      color: 'var(--primary)',
                      border: '1px solid var(--primary)'
                    }}
                  >
                    Active
                  </span>
                </div>
                <p className="text-sm mb-4" style={{ color: 'var(--muted-foreground)' }}>
                  {project.description || 'No description'}
                </p>
                <div className="flex items-center gap-4 text-xs" style={{ color: 'var(--muted-foreground)' }}>
                  <span>🌍 {project.environments?.length || 0} environments</span>
                </div>
                <div className="mt-4 pt-4" style={{ borderTop: '1px solid var(--border)' }}>
                  <div className="flex gap-2">
                    <button
                      className="flex-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors hover:opacity-90"
                      style={{ 
                        backgroundColor: 'var(--primary)', 
                        color: 'var(--primary-foreground)' 
                      }}
                    >
                      Deploy
                    </button>
                    <button
                      className="flex-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors hover:opacity-90"
                      style={{ 
                        border: '1px solid var(--border)',
                        color: 'var(--foreground)'
                      }}
                    >
                      Settings
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

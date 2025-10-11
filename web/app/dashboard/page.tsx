'use client';

import { useEffect, useState } from 'react';
import { apiFetch, API_URL } from '@/lib/api';
import StatCard from './components/StatCard';
import TaskItem from './components/TaskItem';
import SubscriptionSelector from './components/SubscriptionSelector';

export default function Dashboard() {
  const [health, setHealth] = useState<any>(null);
  const [projects, setProjects] = useState<any[]>([]);
  const [apiUrl, setApiUrl] = useState<string>('');

  useEffect(() => {
    setApiUrl(API_URL);
    
    apiFetch('/health')
      .then(setHealth)
      .catch(console.error);

    apiFetch('/projects')
      .then(setProjects)
      .catch(console.error);
  }, []);

  const triggerDeploy = async () => {
    try {
      const data = await apiFetch('/deployments', {
        method: 'POST',
        body: JSON.stringify({
          projectId: '11111111-1111-1111-1111-111111111111',
          envId: '22222222-2222-2222-2222-222222222222',
          templateId: 'demo-template',
          parameters: {}
        })
      });
      alert(`Task created: ${data.taskId}`);
    } catch (error) {
      alert(`Error: ${error}`);
    }
  };

  return (
    <div className="p-8 space-y-8">
      <div className="space-y-4">
        <div>
          <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--foreground)' }}>Dashboard</h1>
          <p style={{ color: 'var(--muted-foreground)' }}>Monitor your cloud operations in real-time</p>
        </div>
        
        <div className="rounded-xl p-4" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <SubscriptionSelector />
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Active Tasks"
          value="12"
          change="+8%"
          icon="🚀"
          trend="up"
        />
        <StatCard
          title="Total Projects"
          value={projects.length}
          change="+2%"
          icon="📦"
          trend="up"
        />
        <StatCard
          title="Deployments Today"
          value="8"
          change="+15%"
          icon="🔄"
          trend="up"
        />
        <StatCard
          title="API Health"
          value={health?.status === 'healthy' ? 'Healthy' : 'Loading...'}
          icon="💚"
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <div className="rounded-xl p-6" style={{ 
            backgroundColor: 'var(--card)', 
            border: '1px solid var(--border)' 
          }}>
            <h2 className="text-2xl font-bold mb-6" style={{ color: 'var(--card-foreground)' }}>Recent Activity</h2>
            <div className="space-y-3">
              <TaskItem
                type="Deployment"
                title="Deploy production v2.5.0"
                status="running"
                timestamp="2 min ago"
                icon="🚀"
              />
              <TaskItem
                type="Database"
                title="Backup PostgreSQL instance"
                status="completed"
                timestamp="15 min ago"
                icon="💾"
              />
              <TaskItem
                type="Kubernetes"
                title="Restart payment-service pods"
                status="completed"
                timestamp="1 hour ago"
                icon="🔄"
              />
              <TaskItem
                type="Sandbox"
                title="Create dev environment"
                status="pending"
                timestamp="2 hours ago"
                icon="🧪"
              />
            </div>
          </div>
        </div>

        <div className="space-y-6">
          <div className="rounded-xl p-6" style={{ 
            backgroundColor: 'var(--card)', 
            border: '1px solid var(--border)' 
          }}>
            <h3 className="text-lg font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>Quick Actions</h3>
            <div className="space-y-3">
              <button 
                onClick={triggerDeploy}
                className="w-full px-4 py-3 rounded-lg font-medium transition-colors flex items-center justify-center gap-2"
                style={{ 
                  backgroundColor: 'var(--primary)', 
                  color: 'var(--primary-foreground)' 
                }}
              >
                <span>🚀</span>
                <span>Deploy Now</span>
              </button>
              <button className="w-full px-4 py-3 rounded-lg font-medium transition-colors flex items-center justify-center gap-2" style={{ 
                backgroundColor: 'var(--secondary)', 
                color: 'var(--secondary-foreground)',
                border: '1px solid var(--border)' 
              }}>
                <span>💾</span>
                <span>Backup DB</span>
              </button>
              <button className="w-full px-4 py-3 rounded-lg font-medium transition-colors flex items-center justify-center gap-2" style={{ 
                backgroundColor: 'var(--secondary)', 
                color: 'var(--secondary-foreground)',
                border: '1px solid var(--border)' 
              }}>
                <span>🔄</span>
                <span>Restart Pods</span>
              </button>
              <button className="w-full px-4 py-3 rounded-lg font-medium transition-colors flex items-center justify-center gap-2" style={{ 
                backgroundColor: 'var(--secondary)', 
                color: 'var(--secondary-foreground)',
                border: '1px solid var(--border)' 
              }}>
                <span>🧪</span>
                <span>New Sandbox</span>
              </button>
            </div>
          </div>

          <div className="rounded-xl p-6" style={{ 
            backgroundColor: 'var(--card)', 
            border: '1px solid var(--border)' 
          }}>
            <h3 className="text-lg font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>System Status</h3>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm" style={{ color: 'var(--muted-foreground)' }}>API Server</span>
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full animate-pulse" style={{ backgroundColor: 'var(--success)' }} />
                  <span className="text-sm font-medium" style={{ color: 'var(--success)' }}>Online</span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Task Queue</span>
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full animate-pulse" style={{ backgroundColor: 'var(--success)' }} />
                  <span className="text-sm font-medium" style={{ color: 'var(--success)' }}>Active</span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Database</span>
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full animate-pulse" style={{ backgroundColor: 'var(--success)' }} />
                  <span className="text-sm font-medium" style={{ color: 'var(--success)' }}>Connected</span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm" style={{ color: 'var(--muted-foreground)' }}>SignalR Hub</span>
                <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full animate-pulse" style={{ backgroundColor: 'var(--success)' }} />
                  <span className="text-sm font-medium" style={{ color: 'var(--success)' }}>Running</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {projects.length > 0 && (
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <h2 className="text-2xl font-bold mb-6" style={{ color: 'var(--card-foreground)' }}>Projects</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {projects.map(project => (
              <div key={project.id} className="p-4 rounded-lg transition-colors" style={{ 
                backgroundColor: 'var(--secondary)', 
                border: '1px solid var(--border)' 
              }}>
                <div className="flex items-start justify-between mb-3">
                  <h3 className="text-lg font-semibold" style={{ color: 'var(--card-foreground)' }}>{project.name}</h3>
                  <span className="px-2 py-1 text-xs font-semibold rounded" style={{ 
                    backgroundColor: 'var(--primary-bg)', 
                    color: 'var(--primary)',
                    border: '1px solid var(--primary)' 
                  }}>
                    Active
                  </span>
                </div>
                <p className="text-sm mb-3" style={{ color: 'var(--muted-foreground)' }}>{project.description}</p>
                <div className="flex items-center gap-2 text-xs" style={{ color: 'var(--muted-foreground)' }}>
                  <span>🌍 {project.environments?.length || 0} environments</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="text-center text-sm">
        <p style={{ color: 'var(--muted-foreground)' }}>
          API: <a href={`${apiUrl}/swagger`} target="_blank" rel="noopener noreferrer" className="hover:underline transition-colors" style={{ color: 'var(--primary)' }}>{apiUrl}/swagger</a>
        </p>
      </div>
    </div>
  );
}

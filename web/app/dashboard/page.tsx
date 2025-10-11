'use client';

import { useEffect, useState } from 'react';
import { apiFetch, API_URL } from '@/lib/api';
import StatCard from './components/StatCard';
import GlassCard from './components/GlassCard';
import TaskItem from './components/TaskItem';

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
      <div>
        <h1 className="text-4xl font-bold text-white mb-2">General Statistics</h1>
        <p className="text-slate-400">Monitor your cloud operations in real-time</p>
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
          <GlassCard>
            <div className="p-6">
              <h2 className="text-2xl font-bold text-white mb-6">Recent Activity</h2>
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
          </GlassCard>
        </div>

        <div className="space-y-6">
          <GlassCard>
            <div className="p-6">
              <h3 className="text-lg font-bold text-white mb-4">Quick Actions</h3>
              <div className="space-y-3">
                <button 
                  onClick={triggerDeploy}
                  className="w-full px-4 py-3 bg-gradient-to-r from-cyan-500 to-blue-500 hover:from-cyan-600 hover:to-blue-600 rounded-xl font-semibold text-white transition-all duration-200 flex items-center justify-center gap-2"
                >
                  <span>🚀</span>
                  <span>Deploy Now</span>
                </button>
                <button className="w-full px-4 py-3 bg-white/5 hover:bg-white/10 border border-white/10 hover:border-cyan-500/50 rounded-xl font-medium text-white transition-all duration-200 flex items-center justify-center gap-2">
                  <span>💾</span>
                  <span>Backup DB</span>
                </button>
                <button className="w-full px-4 py-3 bg-white/5 hover:bg-white/10 border border-white/10 hover:border-cyan-500/50 rounded-xl font-medium text-white transition-all duration-200 flex items-center justify-center gap-2">
                  <span>🔄</span>
                  <span>Restart Pods</span>
                </button>
                <button className="w-full px-4 py-3 bg-white/5 hover:bg-white/10 border border-white/10 hover:border-cyan-500/50 rounded-xl font-medium text-white transition-all duration-200 flex items-center justify-center gap-2">
                  <span>🧪</span>
                  <span>New Sandbox</span>
                </button>
              </div>
            </div>
          </GlassCard>

          <GlassCard>
            <div className="p-6">
              <h3 className="text-lg font-bold text-white mb-4">System Status</h3>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-slate-400 text-sm">API Server</span>
                  <div className="flex items-center gap-2">
                    <div className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" />
                    <span className="text-emerald-400 text-sm font-medium">Online</span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400 text-sm">Task Queue</span>
                  <div className="flex items-center gap-2">
                    <div className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" />
                    <span className="text-emerald-400 text-sm font-medium">Active</span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400 text-sm">Database</span>
                  <div className="flex items-center gap-2">
                    <div className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" />
                    <span className="text-emerald-400 text-sm font-medium">Connected</span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400 text-sm">SignalR Hub</span>
                  <div className="flex items-center gap-2">
                    <div className="w-2 h-2 bg-emerald-400 rounded-full animate-pulse" />
                    <span className="text-emerald-400 text-sm font-medium">Running</span>
                  </div>
                </div>
              </div>
            </div>
          </GlassCard>
        </div>
      </div>

      {projects.length > 0 && (
        <GlassCard>
          <div className="p-6">
            <h2 className="text-2xl font-bold text-white mb-6">Projects</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {projects.map(project => (
                <div key={project.id} className="p-4 rounded-xl bg-white/5 hover:bg-white/10 border border-white/5 hover:border-cyan-500/30 transition-all duration-200">
                  <div className="flex items-start justify-between mb-3">
                    <h3 className="text-lg font-semibold text-white">{project.name}</h3>
                    <span className="px-2 py-1 bg-cyan-500/20 text-cyan-400 text-xs font-semibold rounded-lg border border-cyan-500/30">
                      Active
                    </span>
                  </div>
                  <p className="text-slate-400 text-sm mb-3">{project.description}</p>
                  <div className="flex items-center gap-2 text-xs text-slate-500">
                    <span>🌍 {project.environments?.length || 0} environments</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </GlassCard>
      )}

      <div className="text-center text-slate-500 text-sm">
        <p>API: <a href={`${apiUrl}/swagger`} target="_blank" rel="noopener noreferrer" className="text-cyan-400 hover:text-cyan-300 hover:underline transition-colors">{apiUrl}/swagger</a></p>
      </div>
    </div>
  );
}

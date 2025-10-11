'use client';

import { useEffect, useState } from 'react';
import { apiFetch, API_URL } from '@/lib/api';

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
    <div className="text-white p-8">
      <div className="max-w-7xl mx-auto">
        <h1 className="text-4xl font-bold mb-4 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
          Dashboard
        </h1>
        <p className="text-slate-300 text-lg mb-8">
          Monitor and manage your cloud operations
        </p>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6">
            <h3 className="text-sm font-semibold text-slate-400 mb-2">API Status</h3>
            <p className="text-2xl font-bold">
              {health?.status === 'healthy' ? '✅ Healthy' : '⏳ Loading...'}
            </p>
          </div>

          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6">
            <h3 className="text-sm font-semibold text-slate-400 mb-2">Projects</h3>
            <p className="text-2xl font-bold">{projects.length}</p>
          </div>

          <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6">
            <h3 className="text-sm font-semibold text-slate-400 mb-2">Queue Depth</h3>
            <p className="text-2xl font-bold">0</p>
          </div>
        </div>

        <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6 mb-8">
          <h2 className="text-2xl font-bold mb-4">Quick Actions</h2>
          <div className="flex flex-wrap gap-4">
            <button 
              onClick={triggerDeploy}
              className="px-6 py-3 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 rounded-lg font-semibold transition-all"
            >
              🚀 One-Click Deploy (Demo)
            </button>
            <button className="px-6 py-3 bg-slate-700 hover:bg-slate-600 rounded-lg font-semibold transition-all">
              💾 Backup Database
            </button>
            <button className="px-6 py-3 bg-slate-700 hover:bg-slate-600 rounded-lg font-semibold transition-all">
              🔄 Restart Pods
            </button>
            <button className="px-6 py-3 bg-slate-700 hover:bg-slate-600 rounded-lg font-semibold transition-all">
              🧪 Create Sandbox
            </button>
          </div>
        </div>

        <div className="bg-slate-800/50 backdrop-blur border border-slate-700 rounded-lg p-6">
          <h2 className="text-2xl font-bold mb-4">Projects</h2>
          {projects.length === 0 ? (
            <p className="text-slate-400">No projects yet. Create one to get started!</p>
          ) : (
            projects.map(project => (
              <div key={project.id} className="border-b border-slate-700 last:border-0 py-4">
                <h3 className="text-lg font-semibold">{project.name}</h3>
                <p className="text-slate-400 text-sm">{project.description}</p>
                <div className="mt-2">
                  <span className="text-xs bg-slate-700 px-2 py-1 rounded">
                    {project.environments?.length || 0} environments
                  </span>
                </div>
              </div>
            ))
          )}
        </div>

        <div className="mt-8 text-center text-slate-500 text-sm">
          <p>API: <a href={`${apiUrl}/swagger`} target="_blank" rel="noopener noreferrer" className="text-blue-400 hover:underline">{apiUrl}/swagger</a></p>
        </div>
      </div>
    </div>
  );
}

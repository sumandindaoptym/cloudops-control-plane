'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

export default function DeploymentsPage() {
  const [deployments, setDeployments] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Fetch deployments from API
    apiFetch('/tasks')
      .then((tasks) => {
        const deploymentTasks = tasks.filter((t: any) => t.type === 'Deployment');
        setDeployments(deploymentTasks);
        setLoading(false);
      })
      .catch((error) => {
        console.error('Error fetching deployments:', error);
        setLoading(false);
      });
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
      alert(`Deployment task created: ${data.taskId}`);
      window.location.reload();
    } catch (error) {
      alert(`Error: ${error}`);
    }
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--foreground)' }}>Deployments</h1>
          <p style={{ color: 'var(--muted-foreground)' }}>Manage and monitor your application deployments</p>
        </div>
        <button
          onClick={triggerDeploy}
          className="px-6 py-3 rounded-lg font-medium transition-colors hover:opacity-90"
          style={{ 
            backgroundColor: 'var(--primary)', 
            color: 'var(--primary-foreground)' 
          }}
        >
          🚀 New Deployment
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Total Deployments</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--foreground)' }}>
            {deployments.length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Successful</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--success)' }}>
            {deployments.filter(d => d.status === 'Completed').length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>In Progress</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--primary)' }}>
            {deployments.filter(d => d.status === 'Running').length}
          </div>
        </div>
      </div>

      <div className="rounded-xl p-6" style={{ 
        backgroundColor: 'var(--card)', 
        border: '1px solid var(--border)' 
      }}>
        <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>Recent Deployments</h2>
        
        {loading ? (
          <p style={{ color: 'var(--muted-foreground)' }}>Loading...</p>
        ) : deployments.length === 0 ? (
          <p style={{ color: 'var(--muted-foreground)' }}>No deployments yet. Click "New Deployment" to get started.</p>
        ) : (
          <div className="space-y-3">
            {deployments.map((deployment) => (
              <div 
                key={deployment.id} 
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
                        {deployment.entityId || 'Deployment'}
                      </h3>
                      <span 
                        className="px-2 py-1 text-xs font-semibold rounded"
                        style={{
                          backgroundColor: deployment.status === 'Completed' ? 'var(--success-bg)' : 
                                          deployment.status === 'Running' ? 'var(--primary-bg)' : 
                                          deployment.status === 'Failed' ? 'var(--destructive-bg)' : 'var(--secondary-bg)',
                          color: deployment.status === 'Completed' ? 'var(--success)' : 
                                deployment.status === 'Running' ? 'var(--primary)' : 
                                deployment.status === 'Failed' ? 'var(--destructive)' : 'var(--foreground)',
                          border: `1px solid ${deployment.status === 'Completed' ? 'var(--success)' : 
                                               deployment.status === 'Running' ? 'var(--primary)' : 
                                               deployment.status === 'Failed' ? 'var(--destructive)' : 'var(--border)'}`
                        }}
                      >
                        {deployment.status}
                      </span>
                    </div>
                    <p className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                      {new Date(deployment.createdAt).toLocaleString()}
                    </p>
                  </div>
                  <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
                    ID: {deployment.id.substring(0, 8)}
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

'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

export default function TasksPage() {
  const [tasks, setTasks] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<string>('all');

  useEffect(() => {
    apiFetch('/tasks')
      .then((data) => {
        setTasks(data);
        setLoading(false);
      })
      .catch((error) => {
        console.error('Error fetching tasks:', error);
        setLoading(false);
      });
  }, []);

  const filteredTasks = filter === 'all' 
    ? tasks 
    : tasks.filter(t => t.status.toLowerCase() === filter.toLowerCase());

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--foreground)' }}>Tasks</h1>
          <p style={{ color: 'var(--muted-foreground)' }}>Monitor all background tasks and operations</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setFilter('all')}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors`}
            style={filter === 'all' ? { 
              backgroundColor: 'var(--primary)', 
              color: 'var(--primary-foreground)' 
            } : {
              border: '1px solid var(--border)',
              color: 'var(--foreground)'
            }}
          >
            All
          </button>
          <button
            onClick={() => setFilter('running')}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors`}
            style={filter === 'running' ? { 
              backgroundColor: 'var(--primary)', 
              color: 'var(--primary-foreground)' 
            } : {
              border: '1px solid var(--border)',
              color: 'var(--foreground)'
            }}
          >
            Running
          </button>
          <button
            onClick={() => setFilter('completed')}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors`}
            style={filter === 'completed' ? { 
              backgroundColor: 'var(--primary)', 
              color: 'var(--primary-foreground)' 
            } : {
              border: '1px solid var(--border)',
              color: 'var(--foreground)'
            }}
          >
            Completed
          </button>
          <button
            onClick={() => setFilter('failed')}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors`}
            style={filter === 'failed' ? { 
              backgroundColor: 'var(--primary)', 
              color: 'var(--primary-foreground)' 
            } : {
              border: '1px solid var(--border)',
              color: 'var(--foreground)'
            }}
          >
            Failed
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Total Tasks</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--foreground)' }}>
            {tasks.length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Running</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--primary)' }}>
            {tasks.filter(t => t.status === 'Running').length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Completed</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--success)' }}>
            {tasks.filter(t => t.status === 'Completed').length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Failed</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--destructive)' }}>
            {tasks.filter(t => t.status === 'Failed').length}
          </div>
        </div>
      </div>

      <div className="rounded-xl p-6" style={{ 
        backgroundColor: 'var(--card)', 
        border: '1px solid var(--border)' 
      }}>
        <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>Task History</h2>
        
        {loading ? (
          <p style={{ color: 'var(--muted-foreground)' }}>Loading tasks...</p>
        ) : filteredTasks.length === 0 ? (
          <p style={{ color: 'var(--muted-foreground)' }}>No tasks found.</p>
        ) : (
          <div className="space-y-3">
            {filteredTasks.map((task) => (
              <div 
                key={task.id} 
                className="p-4 rounded-lg transition-colors hover:opacity-90"
                style={{ 
                  backgroundColor: 'var(--secondary)', 
                  border: '1px solid var(--border)' 
                }}
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3">
                      <h3 className="font-semibold" style={{ color: 'var(--card-foreground)' }}>
                        {task.type}
                      </h3>
                      <span 
                        className="px-2 py-1 text-xs font-semibold rounded"
                        style={{
                          backgroundColor: task.status === 'Completed' ? 'var(--success-bg)' : 
                                          task.status === 'Running' ? 'var(--primary-bg)' : 
                                          task.status === 'Failed' ? 'var(--destructive-bg)' : 'var(--secondary-bg)',
                          color: task.status === 'Completed' ? 'var(--success)' : 
                                task.status === 'Running' ? 'var(--primary)' : 
                                task.status === 'Failed' ? 'var(--destructive)' : 'var(--foreground)',
                          border: `1px solid ${task.status === 'Completed' ? 'var(--success)' : 
                                               task.status === 'Running' ? 'var(--primary)' : 
                                               task.status === 'Failed' ? 'var(--destructive)' : 'var(--border)'}`
                        }}
                      >
                        {task.status}
                      </span>
                    </div>
                    <p className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                      Entity: {task.entityId} • Created: {new Date(task.createdAt).toLocaleString()}
                    </p>
                    {task.message && (
                      <p className="text-sm mt-2" style={{ color: 'var(--muted-foreground)' }}>
                        {task.message}
                      </p>
                    )}
                  </div>
                  <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>
                    {task.id.substring(0, 8)}
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

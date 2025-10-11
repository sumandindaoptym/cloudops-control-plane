'use client';

import { useState } from 'react';

export default function DatabasesPage() {
  const [databases] = useState([
    { id: '1', name: 'Production PostgreSQL', type: 'PostgreSQL', status: 'Healthy', size: '2.4 GB', connections: 45 },
    { id: '2', name: 'Staging MySQL', type: 'MySQL', status: 'Healthy', size: '1.1 GB', connections: 12 },
    { id: '3', name: 'Analytics MongoDB', type: 'MongoDB', status: 'Warning', size: '5.2 GB', connections: 8 },
  ]);

  const handleBackup = (dbName: string) => {
    alert(`Backup initiated for ${dbName}`);
  };

  const handleRestore = (dbName: string) => {
    alert(`Restore initiated for ${dbName}`);
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--foreground)' }}>Databases</h1>
          <p style={{ color: 'var(--muted-foreground)' }}>Manage database backups, restores, and monitoring</p>
        </div>
        <button
          className="px-6 py-3 rounded-lg font-medium transition-colors hover:opacity-90"
          style={{ 
            backgroundColor: 'var(--primary)', 
            color: 'var(--primary-foreground)' 
          }}
        >
          💾 New Database
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Total Databases</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--foreground)' }}>
            {databases.length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Healthy</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--success)' }}>
            {databases.filter(d => d.status === 'Healthy').length}
          </div>
        </div>
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <div className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Total Size</div>
          <div className="text-3xl font-bold mt-2" style={{ color: 'var(--foreground)' }}>
            8.7 GB
          </div>
        </div>
      </div>

      <div className="rounded-xl p-6" style={{ 
        backgroundColor: 'var(--card)', 
        border: '1px solid var(--border)' 
      }}>
        <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>Database Instances</h2>
        
        <div className="space-y-3">
          {databases.map((db) => (
            <div 
              key={db.id} 
              className="p-4 rounded-lg transition-colors"
              style={{ 
                backgroundColor: 'var(--secondary)', 
                border: '1px solid var(--border)' 
              }}
            >
              <div className="flex items-center justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-3">
                    <h3 className="font-semibold text-lg" style={{ color: 'var(--card-foreground)' }}>
                      {db.name}
                    </h3>
                    <span 
                      className="px-2 py-1 text-xs font-semibold rounded"
                      style={{
                        backgroundColor: db.status === 'Healthy' ? 'var(--success-bg)' : 'var(--destructive-bg)',
                        color: db.status === 'Healthy' ? 'var(--success)' : 'var(--destructive)',
                        border: `1px solid ${db.status === 'Healthy' ? 'var(--success)' : 'var(--destructive)'}`
                      }}
                    >
                      {db.status}
                    </span>
                  </div>
                  <div className="flex items-center gap-6 mt-2 text-sm" style={{ color: 'var(--muted-foreground)' }}>
                    <span>Type: {db.type}</span>
                    <span>Size: {db.size}</span>
                    <span>Connections: {db.connections}</span>
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleBackup(db.name)}
                    className="px-4 py-2 rounded-lg text-sm font-medium transition-colors hover:opacity-90"
                    style={{ 
                      backgroundColor: 'var(--primary)', 
                      color: 'var(--primary-foreground)' 
                    }}
                  >
                    Backup
                  </button>
                  <button
                    onClick={() => handleRestore(db.name)}
                    className="px-4 py-2 rounded-lg text-sm font-medium transition-colors hover:opacity-90"
                    style={{ 
                      border: '1px solid var(--border)',
                      color: 'var(--foreground)'
                    }}
                  >
                    Restore
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

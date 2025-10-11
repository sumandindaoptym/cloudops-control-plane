'use client';

import { useState } from 'react';

export default function SettingsPage() {
  const [settings, setSettings] = useState({
    notifications: {
      email: true,
      teams: false,
      slack: false,
    },
    deployment: {
      autoApprove: false,
      requireReview: true,
      maxConcurrent: 3,
    },
    backup: {
      autoBackup: true,
      retentionDays: 30,
      frequency: 'daily',
    },
  });

  const handleSave = () => {
    alert('Settings saved successfully!');
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-bold mb-2" style={{ color: 'var(--foreground)' }}>Settings</h1>
          <p style={{ color: 'var(--muted-foreground)' }}>Configure platform preferences and integrations</p>
        </div>
        <button
          onClick={handleSave}
          className="px-6 py-3 rounded-lg font-medium transition-colors hover:opacity-90"
          style={{ 
            backgroundColor: 'var(--primary)', 
            color: 'var(--primary-foreground)' 
          }}
        >
          💾 Save Changes
        </button>
      </div>

      <div className="space-y-6">
        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>Notifications</h2>
          <div className="space-y-4">
            <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div>
                <div className="font-medium" style={{ color: 'var(--card-foreground)' }}>Email Notifications</div>
                <div className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                  Receive deployment updates via email
                </div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.notifications.email}
                  onChange={(e) => setSettings({
                    ...settings,
                    notifications: { ...settings.notifications, email: e.target.checked }
                  })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all" style={{ backgroundColor: settings.notifications.email ? 'var(--primary)' : 'var(--border)' }}></div>
              </label>
            </div>

            <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div>
                <div className="font-medium" style={{ color: 'var(--card-foreground)' }}>Teams Integration</div>
                <div className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                  Send notifications to Microsoft Teams
                </div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.notifications.teams}
                  onChange={(e) => setSettings({
                    ...settings,
                    notifications: { ...settings.notifications, teams: e.target.checked }
                  })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all" style={{ backgroundColor: settings.notifications.teams ? 'var(--primary)' : 'var(--border)' }}></div>
              </label>
            </div>

            <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div>
                <div className="font-medium" style={{ color: 'var(--card-foreground)' }}>Slack Integration</div>
                <div className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                  Post updates to Slack channels
                </div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.notifications.slack}
                  onChange={(e) => setSettings({
                    ...settings,
                    notifications: { ...settings.notifications, slack: e.target.checked }
                  })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all" style={{ backgroundColor: settings.notifications.slack ? 'var(--primary)' : 'var(--border)' }}></div>
              </label>
            </div>
          </div>
        </div>

        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>Deployment Settings</h2>
          <div className="space-y-4">
            <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div>
                <div className="font-medium" style={{ color: 'var(--card-foreground)' }}>Auto-approve Deployments</div>
                <div className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                  Automatically approve non-production deployments
                </div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.deployment.autoApprove}
                  onChange={(e) => setSettings({
                    ...settings,
                    deployment: { ...settings.deployment, autoApprove: e.target.checked }
                  })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all" style={{ backgroundColor: settings.deployment.autoApprove ? 'var(--primary)' : 'var(--border)' }}></div>
              </label>
            </div>

            <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div>
                <div className="font-medium" style={{ color: 'var(--card-foreground)' }}>Require Code Review</div>
                <div className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                  Require peer review before deployment
                </div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.deployment.requireReview}
                  onChange={(e) => setSettings({
                    ...settings,
                    deployment: { ...settings.deployment, requireReview: e.target.checked }
                  })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all" style={{ backgroundColor: settings.deployment.requireReview ? 'var(--primary)' : 'var(--border)' }}></div>
              </label>
            </div>

            <div className="p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div className="font-medium mb-2" style={{ color: 'var(--card-foreground)' }}>Max Concurrent Deployments</div>
              <input
                type="number"
                value={settings.deployment.maxConcurrent}
                onChange={(e) => setSettings({
                  ...settings,
                  deployment: { ...settings.deployment, maxConcurrent: parseInt(e.target.value) }
                })}
                className="w-full px-4 py-2 rounded-lg"
                style={{ 
                  backgroundColor: 'var(--background)',
                  border: '1px solid var(--border)',
                  color: 'var(--foreground)'
                }}
              />
            </div>
          </div>
        </div>

        <div className="rounded-xl p-6" style={{ 
          backgroundColor: 'var(--card)', 
          border: '1px solid var(--border)' 
        }}>
          <h2 className="text-2xl font-bold mb-4" style={{ color: 'var(--card-foreground)' }}>Backup Settings</h2>
          <div className="space-y-4">
            <div className="flex items-center justify-between p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div>
                <div className="font-medium" style={{ color: 'var(--card-foreground)' }}>Automatic Backups</div>
                <div className="text-sm mt-1" style={{ color: 'var(--muted-foreground)' }}>
                  Enable scheduled database backups
                </div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={settings.backup.autoBackup}
                  onChange={(e) => setSettings({
                    ...settings,
                    backup: { ...settings.backup, autoBackup: e.target.checked }
                  })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 rounded-full peer peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all" style={{ backgroundColor: settings.backup.autoBackup ? 'var(--primary)' : 'var(--border)' }}></div>
              </label>
            </div>

            <div className="p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div className="font-medium mb-2" style={{ color: 'var(--card-foreground)' }}>Retention Period (Days)</div>
              <input
                type="number"
                value={settings.backup.retentionDays}
                onChange={(e) => setSettings({
                  ...settings,
                  backup: { ...settings.backup, retentionDays: parseInt(e.target.value) }
                })}
                className="w-full px-4 py-2 rounded-lg"
                style={{ 
                  backgroundColor: 'var(--background)',
                  border: '1px solid var(--border)',
                  color: 'var(--foreground)'
                }}
              />
            </div>

            <div className="p-4 rounded-lg" style={{ backgroundColor: 'var(--secondary)' }}>
              <div className="font-medium mb-2" style={{ color: 'var(--card-foreground)' }}>Backup Frequency</div>
              <select
                value={settings.backup.frequency}
                onChange={(e) => setSettings({
                  ...settings,
                  backup: { ...settings.backup, frequency: e.target.value }
                })}
                className="w-full px-4 py-2 rounded-lg"
                style={{ 
                  backgroundColor: 'var(--background)',
                  border: '1px solid var(--border)',
                  color: 'var(--foreground)'
                }}
              >
                <option value="hourly">Hourly</option>
                <option value="daily">Daily</option>
                <option value="weekly">Weekly</option>
                <option value="monthly">Monthly</option>
              </select>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

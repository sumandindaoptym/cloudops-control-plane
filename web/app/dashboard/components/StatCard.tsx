interface StatCardProps {
  title: string;
  value: string | number;
  change?: string;
  icon: string;
  trend?: 'up' | 'down';
}

export default function StatCard({ title, value, change, icon, trend }: StatCardProps) {
  return (
    <div className="rounded-xl p-6 transition-colors" style={{ 
      backgroundColor: 'var(--card)', 
      border: '1px solid var(--border)' 
    }}>
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium mb-2" style={{ color: 'var(--muted-foreground)' }}>{title}</p>
          <p className="text-3xl font-bold mb-1" style={{ color: 'var(--card-foreground)' }}>{value}</p>
          {change && (
            <p className="text-sm font-medium" style={{ color: trend === 'up' ? 'var(--success)' : 'var(--destructive)' }}>
              {trend === 'up' ? '↑' : '↓'} {change}
            </p>
          )}
        </div>
        <div className="text-4xl">{icon}</div>
      </div>
    </div>
  );
}

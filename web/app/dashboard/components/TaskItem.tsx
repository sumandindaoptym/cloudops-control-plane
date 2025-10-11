interface TaskItemProps {
  type: string;
  title: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  timestamp: string;
  icon: string;
}

export default function TaskItem({ type, title, status, timestamp, icon }: TaskItemProps) {
  const getStatusStyle = (status: string) => {
    switch (status) {
      case 'running':
        return { backgroundColor: 'var(--primary-bg)', color: 'var(--primary)', border: '1px solid var(--primary)' };
      case 'completed':
        return { backgroundColor: 'var(--success-bg)', color: 'var(--success)', border: '1px solid var(--success)' };
      case 'failed':
        return { backgroundColor: 'var(--destructive-bg)', color: 'var(--destructive)', border: '1px solid var(--destructive)' };
      default:
        return { backgroundColor: 'var(--secondary-bg)', color: 'var(--secondary-foreground)', border: '1px solid var(--border)' };
    }
  };

  return (
    <div className="flex items-center gap-4 p-4 rounded-lg transition-colors" style={{ 
      backgroundColor: 'var(--card)', 
      border: '1px solid var(--border)' 
    }}>
      <div className="text-2xl">{icon}</div>
      <div className="flex-1 min-w-0">
        <p className="font-medium truncate" style={{ color: 'var(--card-foreground)' }}>{title}</p>
        <p className="text-sm" style={{ color: 'var(--muted-foreground)' }}>{type}</p>
      </div>
      <div className="flex items-center gap-3">
        <span className="px-3 py-1 rounded-lg text-xs font-semibold" style={getStatusStyle(status)}>
          {status}
        </span>
        <span className="text-xs whitespace-nowrap" style={{ color: 'var(--muted-foreground)' }}>{timestamp}</span>
      </div>
    </div>
  );
}

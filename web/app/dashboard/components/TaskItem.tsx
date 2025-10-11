interface TaskItemProps {
  type: string;
  title: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  timestamp: string;
  icon: string;
}

export default function TaskItem({ type, title, status, timestamp, icon }: TaskItemProps) {
  const statusColors = {
    pending: 'bg-slate-600/50 text-slate-300 border-slate-600',
    running: 'bg-cyan-500/20 text-cyan-400 border-cyan-500/50',
    completed: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/50',
    failed: 'bg-red-500/20 text-red-400 border-red-500/50',
  };

  return (
    <div className="flex items-center gap-4 p-4 rounded-lg bg-slate-800/50 border border-slate-700 hover:border-cyan-500/50 transition-colors">
      <div className="text-2xl">{icon}</div>
      <div className="flex-1 min-w-0">
        <p className="text-white font-medium truncate">{title}</p>
        <p className="text-slate-400 text-sm">{type}</p>
      </div>
      <div className="flex items-center gap-3">
        <span className={`px-3 py-1 rounded-lg text-xs font-semibold border ${statusColors[status]}`}>
          {status}
        </span>
        <span className="text-slate-500 text-xs whitespace-nowrap">{timestamp}</span>
      </div>
    </div>
  );
}

interface TaskItemProps {
  type: string;
  title: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  timestamp: string;
  icon: string;
}

export default function TaskItem({ type, title, status, timestamp, icon }: TaskItemProps) {
  const statusColors = {
    pending: 'bg-slate-500/20 text-slate-400 border-slate-500/30',
    running: 'bg-cyan-500/20 text-cyan-400 border-cyan-500/30',
    completed: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30',
    failed: 'bg-red-500/20 text-red-400 border-red-500/30',
  };

  return (
    <div className="relative group">
      <div className="absolute inset-0 bg-gradient-to-br from-cyan-500/5 to-blue-500/5 rounded-xl blur-lg group-hover:blur-xl transition-all duration-300" />
      <div className="relative flex items-center gap-4 p-4 rounded-xl bg-slate-900/40 backdrop-blur-xl border border-white/10 hover:border-cyan-500/30 transition-all duration-200">
        <div className="w-10 h-10 bg-gradient-to-br from-cyan-500/20 to-blue-500/20 rounded-xl flex items-center justify-center text-xl border border-white/10">
          {icon}
        </div>
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
    </div>
  );
}

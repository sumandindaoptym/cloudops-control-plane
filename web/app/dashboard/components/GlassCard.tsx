import { ReactNode } from 'react';

interface GlassCardProps {
  children: ReactNode;
  className?: string;
}

export default function GlassCard({ children, className = '' }: GlassCardProps) {
  return (
    <div className={`relative group ${className}`}>
      <div className="absolute inset-0 bg-gradient-to-br from-cyan-500/10 to-blue-500/10 rounded-2xl blur-xl transition-all duration-300" />
      <div className="relative bg-slate-900/40 backdrop-blur-xl border border-white/10 rounded-2xl hover:border-cyan-500/30 transition-all duration-300">
        {children}
      </div>
    </div>
  );
}

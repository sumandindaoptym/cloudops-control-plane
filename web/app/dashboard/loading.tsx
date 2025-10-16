export default function DashboardLoading() {
  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="flex flex-col items-center gap-4">
        <div className="relative w-16 h-16">
          <div 
            className="absolute inset-0 rounded-full border-4 opacity-25"
            style={{ borderColor: 'var(--primary)' }}
          />
          <div 
            className="absolute inset-0 rounded-full border-4 border-transparent animate-spin"
            style={{ 
              borderTopColor: 'var(--primary)',
              borderRightColor: 'var(--primary)'
            }}
          />
        </div>
        <p className="text-lg font-medium" style={{ color: 'var(--muted-foreground)' }}>
          Loading...
        </p>
      </div>
    </div>
  );
}

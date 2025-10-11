# Glassmorphism Dashboard - Component Guide

## Overview
The CloudOps Control Plane dashboard features a modern iOS 26-inspired glassmorphism/liquid-glass design with a dark blue gradient background and cyan accent colors.

## Color Scheme
- **Background**: `bg-gradient-to-br from-slate-950 via-blue-950 to-slate-950`
- **Primary Action**: `bg-cyan-500 hover:bg-cyan-600`
- **Glass Background**: `bg-slate-900/40`
- **Borders**: `border-white/10` with `hover:border-cyan-500/30`
- **Text**: White headings, `text-slate-400` for secondary text

## Glassmorphism Components

### StatCard
**Location**: `components/StatCard.tsx`

Displays statistics with trend indicators. Features:
- Gradient glow background (`from-cyan-500/20 to-blue-500/20` with blur)
- `backdrop-blur-xl` for glass effect
- Transparent background (`bg-slate-900/40`)
- Icon with gradient background
- Trend indicators (up/down arrows)

**Usage**:
```tsx
<StatCard
  title="Active Tasks"
  value="12"
  change="+8%"
  icon="🚀"
  trend="up"
/>
```

### GlassCard
**Location**: `components/GlassCard.tsx`

Reusable glass container for any content. Features:
- Gradient glow overlay
- `backdrop-blur-xl` effect
- Transparent slate background
- Subtle white borders with cyan hover state

**Usage**:
```tsx
<GlassCard>
  <div className="p-6">
    {/* Your content here */}
  </div>
</GlassCard>
```

### TaskItem
**Location**: `components/TaskItem.tsx`

Activity feed items with status indicators. Features:
- Full glassmorphism effects (gradient glow + backdrop-blur)
- Status badges with color coding (pending/running/completed/failed)
- Icon display
- Timestamp and type labels

**Usage**:
```tsx
<TaskItem
  type="Deployment"
  title="Deploy production v2.5.0"
  status="running"
  timestamp="2 min ago"
  icon="🚀"
/>
```

### Sidebar
**Location**: `components/Sidebar.tsx`

Navigation sidebar with glassmorphism styling. Features:
- Glass container with gradient glow
- Active state highlighting with cyan background
- Badge support for notification counts
- System status indicator at bottom

**Navigation Items**:
- Overview
- Deployments
- Databases
- Tasks (with badge)
- Projects
- Environments
- Settings

## Dashboard Layout

### Structure
```
DashboardLayout (layout.tsx)
├── DashboardNav (top navigation bar)
└── Main Content Area
    ├── Sidebar (left navigation)
    └── Dashboard Page (main content)
```

### Dashboard Page Sections
1. **Header**: Title and description
2. **Stats Grid**: 4 StatCard components showing key metrics
3. **Main Content**:
   - Recent Activity (2/3 width): Task list with glassmorphism
   - Sidebar Panels (1/3 width):
     - Quick Actions: Buttons for common operations
     - System Status: Real-time service status indicators
4. **Projects Grid**: Display all projects with environment counts

## Visual Effects

### Glassmorphism Pattern
All glass components follow this pattern:
```tsx
<div className="relative group">
  {/* Gradient glow background */}
  <div className="absolute inset-0 bg-gradient-to-br from-cyan-500/20 to-blue-500/20 rounded-2xl blur-xl group-hover:blur-2xl transition-all duration-300" />
  
  {/* Glass content */}
  <div className="relative bg-slate-900/40 backdrop-blur-xl border border-white/10 rounded-2xl hover:border-cyan-500/50 transition-all duration-300">
    {/* Content */}
  </div>
</div>
```

### Hover Effects
- Borders change from `border-white/10` to `border-cyan-500/30` or `border-cyan-500/50`
- Gradient glows intensify from `blur-xl` to `blur-2xl`
- Smooth transitions with `transition-all duration-200` or `duration-300`

## Responsive Design
- Stats grid: 1 column (mobile) → 2 columns (md) → 4 columns (lg)
- Main content: 1 column (mobile) → 2 columns (lg) with 2:1 ratio
- Projects grid: 1 column (mobile) → 2 columns (md) → 3 columns (lg)

## Integration with Backend
The dashboard fetches data from:
- `/api/health` - API health status
- `/api/projects` - Project list
- `/api/deployments` - Deployment operations (POST)

All API calls use the `apiFetch` utility from `@/lib/api`.

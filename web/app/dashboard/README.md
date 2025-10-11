# Olympus Theme Dashboard - Component Guide

## Overview
The CloudOps Control Plane dashboard features a clean, minimal design matching the Olympus brand guidelines with a solid dark background and bright cyan accent colors.

## Color Scheme
- **Background**: `bg-slate-900` (solid dark background)
- **Cards**: `bg-slate-800/50` with `border-slate-700`
- **Primary Action**: `bg-cyan-500 hover:bg-cyan-600`
- **Text**: White headings, `text-slate-400` for secondary text
- **Icons**: Cyan color (`text-cyan-500`)

## Dashboard Components

### StatCard
**Location**: `components/StatCard.tsx`

Clean statistics cards with trend indicators. Features:
- Simple card with border
- Transparent background (`bg-slate-800/50`)
- Border hover effect (cyan)
- Icon display
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

### TaskItem
**Location**: `components/TaskItem.tsx`

Activity feed items with status indicators. Features:
- Clean card design with borders
- Status badges with color coding (pending/running/completed/failed)
- Icon display
- Timestamp and type labels
- Hover effect with cyan border

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

Navigation sidebar with clean styling. Features:
- Simple background (`bg-slate-800/50`)
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
   - Recent Activity (2/3 width): Task list
   - Sidebar Panels (1/3 width):
     - Quick Actions: Buttons for common operations
     - System Status: Real-time service status indicators
4. **Projects Grid**: Display all projects with environment counts

## Visual Effects

### Card Pattern
All cards follow this simple pattern:
```tsx
<div className="bg-slate-800/50 border border-slate-700 rounded-xl p-6 hover:border-cyan-500/50 transition-colors">
  {/* Content */}
</div>
```

### Hover Effects
- Borders change from `border-slate-700` to `border-cyan-500/50`
- Buttons use `hover:bg-cyan-600` for primary actions
- Smooth transitions with `transition-colors`

## Button Styles

### Primary Button
```tsx
<button className="bg-cyan-500 hover:bg-cyan-600 text-white font-medium rounded-lg transition-colors">
  Primary Action
</button>
```

### Secondary Button
```tsx
<button className="bg-slate-700 hover:bg-slate-600 border border-slate-600 text-white font-medium rounded-lg transition-colors">
  Secondary Action
</button>
```

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

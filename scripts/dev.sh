#!/bin/bash

set -e

echo "Starting CloudOps Control Plane in development mode..."
echo "=================================================="

export DEMO_MODE=true
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "Starting API on port 5056 (with integrated Worker)..."
(cd "$PROJECT_ROOT/services/api/CloudOps.Api" && dotnet run --no-hot-reload) &
API_PID=$!

echo "Starting Next.js frontend on port 5000..."
(cd "$PROJECT_ROOT/web" && pnpm dev --port 5000) &
WEB_PID=$!

echo ""
echo "Services started:"
echo "  - API + Worker: http://localhost:5056 (Swagger: http://localhost:5056/swagger)"
echo "  - Web:          http://localhost:5000"
echo ""
echo "Press Ctrl+C to stop all services"

wait

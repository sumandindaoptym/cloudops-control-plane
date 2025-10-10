.PHONY: install dev lint test seed build clean restore run-api run-worker run-web

install:
	@echo "Installing dependencies..."
	pnpm install --prefix web
	dotnet restore

dev:
	@echo "Starting development environment..."
	bash scripts/dev.sh

restore:
	@echo "Restoring .NET packages..."
	dotnet restore

build:
	@echo "Building all projects..."
	dotnet build
	cd web && pnpm build

run-api:
	@echo "Running API on port 5056..."
	cd services/api/CloudOps.Api && dotnet run --no-hot-reload

run-worker:
	@echo "Running Worker..."
	cd services/worker/CloudOps.Worker && dotnet run

run-web:
	@echo "Running Next.js on port 5000..."
	cd web && pnpm dev --port 5000

test:
	@echo "Running tests..."
	dotnet test

lint:
	@echo "Running linters..."
	cd web && pnpm lint
	dotnet format

seed:
	@echo "Seeding database..."
	@echo "Database auto-seeds on first run"

clean:
	@echo "Cleaning build artifacts..."
	dotnet clean
	rm -rf web/.next
	rm -rf web/out
	find . -type d -name "bin" -o -name "obj" | xargs rm -rf

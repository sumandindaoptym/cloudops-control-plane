#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
AGENT_PROJECT="$PROJECT_ROOT/services/agent/CloudOps.Agent"
OUTPUT_DIR="$PROJECT_ROOT/artifacts/agents"
VERSION="${VERSION:-1.0.0}"

echo "========================================"
echo "CloudOps Agent Packaging Script"
echo "Version: $VERSION"
echo "========================================"

command -v dotnet >/dev/null 2>&1 || { echo "Error: dotnet is required but not installed."; exit 1; }

if [ ! -f "$AGENT_PROJECT/CloudOps.Agent.csproj" ]; then
    echo "Error: Agent project not found at $AGENT_PROJECT"
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

rm -rf "$OUTPUT_DIR"/*

echo ""
echo "Building Linux x64..."
echo "----------------------------------------"
dotnet publish "$AGENT_PROJECT" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:Version=$VERSION \
    -o "$OUTPUT_DIR/linux-x64"

cp "$PROJECT_ROOT/scripts/install-agent-linux.sh" "$OUTPUT_DIR/linux-x64/install.sh" 2>/dev/null || true
chmod +x "$OUTPUT_DIR/linux-x64/cloudops-agent"

echo "Creating cloudops-agent-linux-x64.tar.gz..."
tar -czvf "$OUTPUT_DIR/cloudops-agent-linux-x64.tar.gz" \
    -C "$OUTPUT_DIR/linux-x64" \
    .

echo ""
echo "Building Windows x64..."
echo "----------------------------------------"
dotnet publish "$AGENT_PROJECT" \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:Version=$VERSION \
    -o "$OUTPUT_DIR/win-x64"

cp "$PROJECT_ROOT/scripts/install-agent-windows.ps1" "$OUTPUT_DIR/win-x64/install.ps1" 2>/dev/null || true

echo "Creating cloudops-agent-windows-x64.zip..."
cd "$OUTPUT_DIR/win-x64"
zip -r "$OUTPUT_DIR/cloudops-agent-windows-x64.zip" .

echo ""
echo "Packaging Helm chart..."
echo "----------------------------------------"
if [ -d "$PROJECT_ROOT/deploy/helm/cloudops-agent" ]; then
    if command -v helm >/dev/null 2>&1; then
        helm package "$PROJECT_ROOT/deploy/helm/cloudops-agent" \
            --version "$VERSION" \
            --app-version "$VERSION" \
            --destination "$OUTPUT_DIR"
    else
        echo "Warning: helm not found, skipping Helm chart packaging"
        echo "To create Helm package manually: helm package deploy/helm/cloudops-agent"
    fi
fi

echo ""
echo "Copying Kubernetes manifests..."
echo "----------------------------------------"
if [ -f "$PROJECT_ROOT/deploy/k8s/cloudops-agent-k8s-manifests.yaml" ]; then
    cp "$PROJECT_ROOT/deploy/k8s/cloudops-agent-k8s-manifests.yaml" "$OUTPUT_DIR/"
fi

echo ""
echo "========================================"
echo "Package Summary"
echo "========================================"
echo ""
ls -lh "$OUTPUT_DIR"/*.tar.gz "$OUTPUT_DIR"/*.zip "$OUTPUT_DIR"/*.tgz "$OUTPUT_DIR"/*.yaml 2>/dev/null || true
echo ""
echo "Artifacts ready in: $OUTPUT_DIR"

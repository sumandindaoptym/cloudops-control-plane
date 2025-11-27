#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
AGENT_DIR="$PROJECT_ROOT/services/agent"

VERSION="${VERSION:-1.0.0}"
REGISTRY="${REGISTRY:-your-acr.azurecr.io}"
IMAGE_NAME="${IMAGE_NAME:-cloudops-agent}"
FULL_IMAGE="$REGISTRY/$IMAGE_NAME"

echo "========================================"
echo "CloudOps Agent Container Build"
echo "========================================"
echo "Version:  $VERSION"
echo "Registry: $REGISTRY"
echo "Image:    $FULL_IMAGE"
echo "========================================"
echo ""

cd "$AGENT_DIR"

echo "Building container image..."
docker build \
    --build-arg VERSION=$VERSION \
    -t "$FULL_IMAGE:$VERSION" \
    -t "$FULL_IMAGE:latest" \
    -f Dockerfile \
    .

echo ""
echo "Image built successfully!"
echo ""
echo "To push to ACR:"
echo "  1. Login: az acr login --name your-acr"
echo "  2. Push:  docker push $FULL_IMAGE:$VERSION"
echo "            docker push $FULL_IMAGE:latest"
echo ""
echo "To run locally:"
echo "  docker run -e API_URL=https://your-api -e API_KEY=your-key -e POOL_ID=your-pool $FULL_IMAGE:$VERSION"

# CloudOps Agent Downloads

This directory contains downloadable agent artifacts.

## Building the Artifacts

To build the agent binaries, run the following script from the project root:

```bash
./scripts/package-agent.sh
```

This will create:
- `cloudops-agent-linux-x64.tar.gz` - Linux binary package
- `cloudops-agent-windows-x64.zip` - Windows binary package
- `cloudops-agent-helm-chart.tgz` - Helm chart package

## Available Files

- `cloudops-agent-k8s-manifests.yaml` - Kubernetes deployment manifests
- `Dockerfile` - Container build file

## CI/CD Integration

For automated builds, use:
```bash
VERSION=1.0.0 ./scripts/package-agent.sh
```

The artifacts will be placed in `artifacts/agents/` and should be copied to this directory for web serving.

# CloudOps Agent Dockerfile
# Multi-stage build for optimized container image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ARG VERSION=1.0.0

COPY ["CloudOps.Agent/CloudOps.Agent.csproj", "CloudOps.Agent/"]
RUN dotnet restore "CloudOps.Agent/CloudOps.Agent.csproj"

COPY . .
WORKDIR /src/CloudOps.Agent

RUN dotnet publish "CloudOps.Agent.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -p:Version=$VERSION \
    -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r cloudops && useradd -r -g cloudops cloudops

COPY --from=build /app/publish .

RUN mkdir -p /app/work /app/logs && \
    chown -R cloudops:cloudops /app

USER cloudops

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

LABEL org.opencontainers.image.title="CloudOps Agent" \
      org.opencontainers.image.description="Self-hosted agent for CloudOps Control Plane" \
      org.opencontainers.image.vendor="Optym" \
      org.opencontainers.image.source="https://github.com/optym/cloudops"

ENTRYPOINT ["dotnet", "cloudops-agent.dll"]
CMD ["run", "--url", "${API_URL}", "--api-key", "${API_KEY}", "--pool", "${POOL_ID}"]

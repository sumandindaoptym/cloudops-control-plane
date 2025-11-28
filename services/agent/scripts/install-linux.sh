#!/bin/bash
set -e

INSTALL_DIR="${INSTALL_DIR:-/opt/cloudops-agent}"
SERVICE_USER="${SERVICE_USER:-cloudops}"
CONFIG_FILE="${CONFIG_FILE:-/etc/cloudops-agent/appsettings.json}"

echo "========================================"
echo "CloudOps Agent Installation"
echo "========================================"
echo ""

if [ "$EUID" -ne 0 ]; then
    echo "Error: This script must be run as root (use sudo)"
    exit 1
fi

echo "Creating service user..."
if ! id "$SERVICE_USER" &>/dev/null; then
    useradd -r -s /bin/false "$SERVICE_USER"
    echo "Created user: $SERVICE_USER"
else
    echo "User $SERVICE_USER already exists"
fi

echo "Creating directories..."
mkdir -p "$INSTALL_DIR"
mkdir -p "$(dirname $CONFIG_FILE)"
mkdir -p /var/log/cloudops-agent
mkdir -p /var/lib/cloudops-agent/work

echo "Copying agent files..."
cp -r ./* "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/CloudOps.Agent"

if [ ! -f "$CONFIG_FILE" ]; then
    echo ""
    echo "Creating configuration file..."
    
    read -p "Enter CloudOps API URL: " API_URL
    read -p "Enter Agent API Key: " API_KEY
    read -p "Enter Agent Pool ID: " POOL_ID
    read -p "Enter Agent Name [$(hostname)]: " AGENT_NAME
    AGENT_NAME="${AGENT_NAME:-$(hostname)}"
    
    cat > "$CONFIG_FILE" << EOF
{
  "Agent": {
    "ApiUrl": "$API_URL",
    "ApiKey": "$API_KEY",
    "PoolId": "$POOL_ID",
    "AgentName": "$AGENT_NAME",
    "MaxParallelJobs": 2,
    "HeartbeatIntervalSeconds": 30,
    "JobPollIntervalSeconds": 5,
    "WorkDirectory": "/var/lib/cloudops-agent/work"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/cloudops-agent/agent-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
EOF
    chmod 600 "$CONFIG_FILE"
fi

chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"
chown -R "$SERVICE_USER:$SERVICE_USER" /var/log/cloudops-agent
chown -R "$SERVICE_USER:$SERVICE_USER" /var/lib/cloudops-agent

echo "Creating systemd service..."
cat > /etc/systemd/system/cloudops-agent.service << EOF
[Unit]
Description=CloudOps Agent
After=network.target

[Service]
Type=simple
User=$SERVICE_USER
Group=$SERVICE_USER
WorkingDirectory=$INSTALL_DIR
ExecStart=$INSTALL_DIR/CloudOps.Agent --config $CONFIG_FILE
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal
SyslogIdentifier=cloudops-agent

Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1

[Install]
WantedBy=multi-user.target
EOF

echo "Reloading systemd..."
systemctl daemon-reload

echo ""
echo "========================================"
echo "Installation Complete!"
echo "========================================"
echo ""
echo "To start the agent:"
echo "  sudo systemctl start cloudops-agent"
echo ""
echo "To enable on boot:"
echo "  sudo systemctl enable cloudops-agent"
echo ""
echo "To view logs:"
echo "  sudo journalctl -u cloudops-agent -f"
echo ""
echo "Configuration file: $CONFIG_FILE"
echo ""

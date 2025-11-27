#!/bin/bash
set -e

INSTALL_DIR="${INSTALL_DIR:-/opt/cloudops-agent}"
SERVICE_USER="${SERVICE_USER:-cloudops}"

echo "CloudOps Agent Linux Installer"
echo "==============================="
echo ""

if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (sudo)"
    exit 1
fi

echo "Creating service user: $SERVICE_USER"
id -u $SERVICE_USER &>/dev/null || useradd -r -s /bin/false $SERVICE_USER

echo "Creating installation directory: $INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR/work"
mkdir -p "$INSTALL_DIR/logs"

echo "Copying agent files..."
cp cloudops-agent "$INSTALL_DIR/"
cp appsettings.json "$INSTALL_DIR/" 2>/dev/null || true
chmod +x "$INSTALL_DIR/cloudops-agent"

chown -R $SERVICE_USER:$SERVICE_USER "$INSTALL_DIR"

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
ExecStart=$INSTALL_DIR/cloudops-agent run --url \${API_URL} --api-key \${API_KEY} --pool \${POOL_ID}
EnvironmentFile=-/etc/cloudops-agent/agent.env
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF

echo "Creating environment file template..."
mkdir -p /etc/cloudops-agent
cat > /etc/cloudops-agent/agent.env << EOF
# CloudOps Agent Configuration
# Edit this file with your actual values

API_URL=https://your-cloudops-api.example.com
API_KEY=your-api-key-here
POOL_ID=your-pool-id-here
EOF

chmod 600 /etc/cloudops-agent/agent.env

systemctl daemon-reload

echo ""
echo "Installation complete!"
echo ""
echo "Next steps:"
echo "1. Edit /etc/cloudops-agent/agent.env with your configuration"
echo "2. Start the agent: sudo systemctl start cloudops-agent"
echo "3. Enable on boot: sudo systemctl enable cloudops-agent"
echo "4. Check status: sudo systemctl status cloudops-agent"
echo "5. View logs: sudo journalctl -u cloudops-agent -f"

#Requires -RunAsAdministrator

param(
    [string]$InstallDir = "C:\Program Files\CloudOps\Agent",
    [string]$ConfigFile = "C:\ProgramData\CloudOps\Agent\appsettings.json"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================"
Write-Host "CloudOps Agent Installation"
Write-Host "========================================"
Write-Host ""

Write-Host "Creating directories..."
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $ConfigFile) | Out-Null
New-Item -ItemType Directory -Force -Path "C:\ProgramData\CloudOps\Agent\logs" | Out-Null
New-Item -ItemType Directory -Force -Path "C:\ProgramData\CloudOps\Agent\work" | Out-Null

Write-Host "Copying agent files..."
Copy-Item -Path ".\*" -Destination $InstallDir -Recurse -Force

if (-not (Test-Path $ConfigFile)) {
    Write-Host ""
    Write-Host "Creating configuration file..."
    
    $apiUrl = Read-Host "Enter CloudOps API URL"
    $apiKey = Read-Host "Enter Agent API Key"
    $poolId = Read-Host "Enter Agent Pool ID"
    $agentName = Read-Host "Enter Agent Name [$env:COMPUTERNAME]"
    if ([string]::IsNullOrEmpty($agentName)) {
        $agentName = $env:COMPUTERNAME
    }
    
    $config = @{
        Agent = @{
            ApiUrl = $apiUrl
            ApiKey = $apiKey
            PoolId = $poolId
            AgentName = $agentName
            MaxParallelJobs = 2
            HeartbeatIntervalSeconds = 30
            JobPollIntervalSeconds = 5
            WorkDirectory = "C:\ProgramData\CloudOps\Agent\work"
        }
        Serilog = @{
            MinimumLevel = @{
                Default = "Information"
            }
            WriteTo = @(
                @{ Name = "Console" }
                @{
                    Name = "File"
                    Args = @{
                        path = "C:\ProgramData\CloudOps\Agent\logs\agent-.log"
                        rollingInterval = "Day"
                        retainedFileCountLimit = 7
                    }
                }
            )
        }
    }
    
    $config | ConvertTo-Json -Depth 10 | Set-Content -Path $ConfigFile
}

Write-Host "Installing Windows Service..."

$serviceName = "CloudOpsAgent"
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $serviceName -Force
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

$exePath = Join-Path $InstallDir "CloudOps.Agent.exe"
$binPath = "`"$exePath`" --config `"$ConfigFile`""

New-Service -Name $serviceName `
    -DisplayName "CloudOps Agent" `
    -Description "CloudOps Control Plane Agent - executes jobs from the CloudOps platform" `
    -BinaryPathName $binPath `
    -StartupType Automatic `
    | Out-Null

Write-Host ""
Write-Host "========================================"
Write-Host "Installation Complete!"
Write-Host "========================================"
Write-Host ""
Write-Host "To start the agent:"
Write-Host "  Start-Service CloudOpsAgent"
Write-Host ""
Write-Host "To view service status:"
Write-Host "  Get-Service CloudOpsAgent"
Write-Host ""
Write-Host "To view logs:"
Write-Host "  Get-Content 'C:\ProgramData\CloudOps\Agent\logs\agent-*.log' -Tail 50"
Write-Host ""
Write-Host "Configuration file: $ConfigFile"
Write-Host ""

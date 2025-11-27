#Requires -RunAsAdministrator

param(
    [string]$InstallDir = "C:\CloudOps\Agent",
    [string]$ApiUrl = "",
    [string]$ApiKey = "",
    [string]$PoolId = ""
)

Write-Host "CloudOps Agent Windows Installer" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Creating installation directory: $InstallDir"
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
New-Item -ItemType Directory -Force -Path "$InstallDir\work" | Out-Null
New-Item -ItemType Directory -Force -Path "$InstallDir\logs" | Out-Null

Write-Host "Copying agent files..."
Copy-Item "cloudops-agent.exe" -Destination $InstallDir -Force
Copy-Item "appsettings.json" -Destination $InstallDir -Force -ErrorAction SilentlyContinue

$serviceName = "CloudOpsAgent"
$serviceDisplayName = "CloudOps Agent"
$serviceDescription = "Self-hosted agent for CloudOps Control Plane"

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $serviceName -Force
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creating Windows service..."

$binPath = "`"$InstallDir\cloudops-agent.exe`" run --url `"$ApiUrl`" --api-key `"$ApiKey`" --pool `"$PoolId`""

New-Service -Name $serviceName `
    -BinaryPathName $binPath `
    -DisplayName $serviceDisplayName `
    -Description $serviceDescription `
    -StartupType Automatic `
    -ErrorAction Stop

$acl = Get-Acl "$InstallDir"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT SERVICE\$serviceName", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl -Path "$InstallDir" -AclObject $acl

Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Service name: $serviceName"
Write-Host "Install location: $InstallDir"
Write-Host ""
Write-Host "Commands:" -ForegroundColor Yellow
Write-Host "  Start service:   Start-Service $serviceName"
Write-Host "  Stop service:    Stop-Service $serviceName"
Write-Host "  Check status:    Get-Service $serviceName"
Write-Host "  View logs:       Get-Content '$InstallDir\logs\cloudops-agent-*.log' -Wait"
Write-Host ""

if ($ApiUrl -and $ApiKey -and $PoolId) {
    Write-Host "Starting service with provided configuration..."
    Start-Service -Name $serviceName
    Write-Host "Service started!" -ForegroundColor Green
} else {
    Write-Host "To start the service, run:" -ForegroundColor Yellow
    Write-Host "  .\install.ps1 -ApiUrl 'https://your-api' -ApiKey 'your-key' -PoolId 'your-pool-id'"
}

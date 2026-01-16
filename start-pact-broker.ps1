# Start/Manage Self-Hosted Pact Broker
# Usage: .\start-pact-broker.ps1 [-Action start|stop|restart|logs|status|clean]

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('start', 'stop', 'restart', 'logs', 'status', 'clean')]
    [string]$Action = 'start'
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PACT BROKER MANAGEMENT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

function Start-PactBroker {
    Write-Host "Starting Pact Broker and PostgreSQL..." -ForegroundColor White
    Write-Host ""
    
    docker-compose up -d postgres pact-broker
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Pact Broker services started" -ForegroundColor Green
        Write-Host ""
        Write-Host "Waiting for services to be healthy..." -ForegroundColor Yellow
        Start-Sleep -Seconds 15
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "PACT BROKER IS READY!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Web UI: " -NoNewline
        Write-Host "http://localhost:9292" -ForegroundColor Cyan
        Write-Host "Username: " -NoNewline
        Write-Host "admin" -ForegroundColor Yellow
        Write-Host "Password: " -NoNewline
        Write-Host "admin" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "API Documentation: http://localhost:9292/doc" -ForegroundColor Gray
        Write-Host "Health Check: http://localhost:9292/diagnostic/status/heartbeat" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "[ERROR] Failed to start Pact Broker" -ForegroundColor Red
        exit 1
    }
}

function Stop-PactBroker {
    Write-Host "Stopping Pact Broker services..." -ForegroundColor White
    Write-Host ""
    
    docker-compose stop postgres pact-broker
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Pact Broker services stopped" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Failed to stop Pact Broker" -ForegroundColor Red
        exit 1
    }
}

function Restart-PactBroker {
    Write-Host "Restarting Pact Broker services..." -ForegroundColor White
    Write-Host ""
    
    docker-compose restart postgres pact-broker
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Pact Broker services restarted" -ForegroundColor Green
        Write-Host ""
        Write-Host "Pact Broker UI: http://localhost:9292" -ForegroundColor Cyan
    } else {
        Write-Host "[ERROR] Failed to restart Pact Broker" -ForegroundColor Red
        exit 1
    }
}

function Show-Logs {
    Write-Host "Showing Pact Broker logs (Ctrl+C to exit)..." -ForegroundColor White
    Write-Host ""
    
    docker-compose logs -f pact-broker
}

function Show-Status {
    Write-Host "Checking Pact Broker status..." -ForegroundColor White
    Write-Host ""
    
    $postgresStatus = docker ps --filter "name=pact-broker-postgres" --filter "status=running" --format "{{.Status}}"
    $brokerStatus = docker ps --filter "name=pact-broker" --filter "status=running" --format "{{.Status}}"
    
    Write-Host "Services Status:" -ForegroundColor White
    Write-Host ""
    
    if ($postgresStatus) {
        Write-Host "  [OK] PostgreSQL: " -NoNewline -ForegroundColor Green
        Write-Host "Running ($postgresStatus)" -ForegroundColor Gray
    } else {
        Write-Host "  [X] PostgreSQL: " -NoNewline -ForegroundColor Red
        Write-Host "Not running" -ForegroundColor Gray
    }
    
    if ($brokerStatus) {
        Write-Host "  [OK] Pact Broker: " -NoNewline -ForegroundColor Green
        Write-Host "Running ($brokerStatus)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  Access: http://localhost:9292" -ForegroundColor Cyan
        
        # Try to check health endpoint
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:9292/diagnostic/status/heartbeat" -Method Get -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "  [OK] Health Check: Healthy" -ForegroundColor Green
            }
        } catch {
            Write-Host "  [!] Health Check: Unable to connect" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  [X] Pact Broker: " -NoNewline -ForegroundColor Red
        Write-Host "Not running" -ForegroundColor Gray
    }
    Write-Host ""
}

function Clean-PactBroker {
    Write-Host "[WARNING] This will remove all Pact Broker data!" -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Are you sure you want to continue? (yes/no)"
    
    if ($confirmation -ne 'yes') {
        Write-Host "Cancelled." -ForegroundColor Gray
        return
    }
    
    Write-Host ""
    Write-Host "Stopping and removing Pact Broker services..." -ForegroundColor White
    
    docker-compose down -v
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Pact Broker services and data removed" -ForegroundColor Green
        Write-Host ""
        Write-Host "To start fresh, run: .\start-pact-broker.ps1 -Action start" -ForegroundColor Cyan
    } else {
        Write-Host "[ERROR] Failed to clean Pact Broker" -ForegroundColor Red
        exit 1
    }
}

# Execute action
switch ($Action) {
    'start' {
        Start-PactBroker
    }
    'stop' {
        Stop-PactBroker
    }
    'restart' {
        Restart-PactBroker
    }
    'logs' {
        Show-Logs
    }
    'status' {
        Show-Status
    }
    'clean' {
        Clean-PactBroker
    }
}

Write-Host ""

param(
    [string]$BrokerUrl = "https://pact-broker.orangeisland-078e5d22.centralus.azurecontainerapps.io",
    [string]$Username = "admin",
    [string]$Password = "admin123!"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PACT BROKER HEALTH CHECK" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "URL: $BrokerUrl" -ForegroundColor Gray
Write-Host ""

# 1. Check Heartbeat (Unauthenticated)
Write-Host "1. Checking Heartbeat..." -NoNewline
try {
    $heartbeatUrl = "$BrokerUrl/diagnostic/status/heartbeat"
    $response = Invoke-WebRequest -Uri $heartbeatUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
    
    if ($response.StatusCode -eq 200) {
        Write-Host " [OK] Healthy" -ForegroundColor Green
    } else {
        Write-Host " [!] Status: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host " [X] Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
         Write-Host "     Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host ""

# 2. Check Authentication & API Access
Write-Host "2. Checking Authentication..." -NoNewline

$auth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("${Username}:${Password}"))
$headers = @{
    "Authorization" = "Basic $auth"
    "Accept" = "application/hal+json"
}

try {
    $response = Invoke-RestMethod -Uri $BrokerUrl -Headers $headers -Method Get -TimeoutSec 10 -ErrorAction Stop
    
    if ($response -and $response._links) {
        Write-Host " [OK] Success" -ForegroundColor Green
        Write-Host ""
        Write-Host "   Authenticated as: $Username" -ForegroundColor Gray
        Write-Host "   API Version: $($response._links.'pb:latest-provider-pacts'.title)" -ForegroundColor Gray
        
        # Check Pacts count
        if ($response._links.'pb:pacticipants') {
             Write-Host "   Pacticipants endpoint available" -ForegroundColor Gray
        }
    } else {
        Write-Host " [!] Valid response but unexpected format" -ForegroundColor Yellow
    }
} catch {
    Write-Host " [X] Failed" -ForegroundColor Red
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Host "     HTTP Status: $code" -ForegroundColor Red
        
        if ($code -eq 401 -or $code -eq 403) {
            Write-Host "     [!] Invalid Credentials or Permission Denied" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CHECK COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

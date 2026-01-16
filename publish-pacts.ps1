# Publish Pact contracts to Pact Broker via REST API
# Usage: .\publish-pacts.ps1 -Version "1.0.0" -Branch "main"

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Branch = "main",
    
    [Parameter(Mandatory=$false)]
    [string]$BrokerUrl = $env:PACT_BROKER_BASE_URL
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PACT BROKER - PUBLISH CONTRACTS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate broker URL
if ([string]::IsNullOrWhiteSpace($BrokerUrl)) {
    Write-Host "[ERROR] PACT_BROKER_BASE_URL not set" -ForegroundColor Red
    Write-Host ""
    Write-Host "Set environment variable:" -ForegroundColor Yellow
    Write-Host "  `$env:PACT_BROKER_BASE_URL='http://localhost:9292'" -ForegroundColor Gray
    exit 1
}

Write-Host "Broker URL: $BrokerUrl" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "Branch: $Branch" -ForegroundColor Cyan
Write-Host ""

# Setup authentication headers
$headers = @{
    "Content-Type" = "application/json"
}

$brokerToken = $env:PACT_BROKER_TOKEN
$brokerUsername = $env:PACT_BROKER_USERNAME
$brokerPassword = $env:PACT_BROKER_PASSWORD

if (![string]::IsNullOrWhiteSpace($brokerToken)) {
    Write-Host "Authentication: Token (PactFlow)" -ForegroundColor Cyan
    $headers["Authorization"] = "Bearer $brokerToken"
}
elseif (![string]::IsNullOrWhiteSpace($brokerUsername) -and ![string]::IsNullOrWhiteSpace($brokerPassword)) {
    Write-Host "Authentication: Basic Auth (Self-hosted)" -ForegroundColor Cyan
    $auth = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("${brokerUsername}:${brokerPassword}"))
    $headers["Authorization"] = "Basic $auth"
}
else {
    Write-Host "[ERROR] No authentication credentials found" -ForegroundColor Red
    Write-Host ""
    Write-Host "For self-hosted, set:" -ForegroundColor Yellow
    Write-Host "  `$env:PACT_BROKER_USERNAME='admin'" -ForegroundColor Gray
    Write-Host "  `$env:PACT_BROKER_PASSWORD='admin'" -ForegroundColor Gray
    exit 1
}

Write-Host ""

function Publish-PactFile {
    param(
        [string]$PactFilePath,
        [string]$Consumer,
        [string]$Provider
    )
    
    try {
        $pactContent = Get-Content $PactFilePath -Raw
        $pactJson = $pactContent | ConvertFrom-Json
        
        # Construct the PUT URL
        $url = "$BrokerUrl/pacts/provider/$Provider/consumer/$Consumer/version/$Version"
        
        Write-Host "  Publishing: $Consumer -> $Provider" -ForegroundColor Gray
        Write-Host "  URL: $url" -ForegroundColor DarkGray
        
        $response = Invoke-RestMethod -Uri $url -Method Put -Headers $headers -Body $pactContent -ContentType "application/json"
        
        Write-Host "  [OK] Published successfully" -ForegroundColor Green
        
        # Tag the version with branch name
        try {
            $tagUrl = "$BrokerUrl/pacticipants/$Consumer/versions/$Version/tags/$Branch"
            Invoke-RestMethod -Uri $tagUrl -Method Put -Headers $headers -ContentType "application/json"
            Write-Host "  [OK] Tagged with branch: $Branch" -ForegroundColor Green
        } catch {
            Write-Host "  [!] Warning: Could not tag with branch" -ForegroundColor Yellow
        }
        
        return $true
    }
    catch {
        Write-Host "  [ERROR] Failed to publish: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Publish Order Service pacts
$orderPactsPath = "tests\OrderService.Contract.Tests\pacts"
$publishedCount = 0

if (Test-Path $orderPactsPath) {
    Write-Host "[*] Publishing OrderService pacts..." -ForegroundColor White
    Write-Host ""
    
    $pactFiles = Get-ChildItem -Path $orderPactsPath -Filter "*.json"
    
    foreach ($pactFile in $pactFiles) {
        $pactJson = Get-Content $pactFile.FullName -Raw | ConvertFrom-Json
        $consumer = $pactJson.consumer.name
        $provider = $pactJson.provider.name
        
        if (Publish-PactFile -PactFilePath $pactFile.FullName -Consumer $consumer -Provider $provider) {
            $publishedCount++
        }
        Write-Host ""
    }
} else {
    Write-Host "[!] OrderService pacts not found at: $orderPactsPath" -ForegroundColor Yellow
    Write-Host "   Run consumer tests first: dotnet test tests\OrderService.Contract.Tests\OrderService.Contract.Tests.csproj --filter 'OrderClientTests'" -ForegroundColor Yellow
    Write-Host ""
}

# Publish Inventory Service pacts
$inventoryPactsPath = "tests\InventoryService.Contract.Tests\pacts"

if (Test-Path $inventoryPactsPath) {
    Write-Host "[*] Publishing InventoryService pacts..." -ForegroundColor White
    Write-Host ""
    
    $pactFiles = Get-ChildItem -Path $inventoryPactsPath -Filter "*.json"
    
    foreach ($pactFile in $pactFiles) {
        $pactJson = Get-Content $pactFile.FullName -Raw | ConvertFrom-Json
        $consumer = $pactJson.consumer.name
        $provider = $pactJson.provider.name
        
        if (Publish-PactFile -PactFilePath $pactFile.FullName -Consumer $consumer -Provider $provider) {
            $publishedCount++
        }
        Write-Host ""
    }
} else {
    Write-Host "[!] InventoryService pacts not found at: $inventoryPactsPath" -ForegroundColor Yellow
    Write-Host "   Run consumer tests first: dotnet test tests\InventoryService.Contract.Tests\InventoryService.Contract.Tests.csproj --filter 'InventoryClientTests'" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PUBLISH COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Published $publishedCount pact(s)" -ForegroundColor Green
Write-Host "View contracts at: $BrokerUrl" -ForegroundColor Cyan
Write-Host ""

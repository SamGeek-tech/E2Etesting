# Check if a service can be deployed via Pact Broker REST API
# Usage: .\can-i-deploy.ps1 -Pacticipant "OrderServiceApi" -Version "1.0.0"

param(
    [Parameter(Mandatory=$true)]
    [string]$Pacticipant,
    
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$To = "production",
    
    [Parameter(Mandatory=$false)]
    [string]$BrokerUrl = $env:PACT_BROKER_BASE_URL
)

# Sanitize inputs (handle accidentally passed equals signs or quotes)
$BrokerUrl = $BrokerUrl.Trim('=', '"', "'", ' ')
$Pacticipant = $Pacticipant.Trim('=', '"', "'", ' ')
$Version = $Version.Trim('=', '"', "'", ' ')

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PACT BROKER - CAN I DEPLOY?" -ForegroundColor Cyan
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
Write-Host "Pacticipant: $Pacticipant" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Cyan
Write-Host "Environment: $To" -ForegroundColor Cyan
Write-Host ""

# Setup authentication headers
$headers = @{
    "Accept" = "application/hal+json"
}

$brokerToken = $env:PACT_BROKER_TOKEN
$brokerUsername = $env:PACT_BROKER_USERNAME
$brokerPassword = $env:PACT_BROKER_PASSWORD

# Handle unexpanded CI variables (e.g. "$(PactBrokerToken)")
if ($brokerToken -like '*$(*') {
    $brokerToken = $null
}

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
    Write-Host "  `$env:PACT_BROKER_PASSWORD='admin123!'" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "Checking deployment readiness..." -ForegroundColor White
Write-Host ""

try {
    # Use the Matrix API to check can-i-deploy
    # This checks if all pacts for this version have been verified
    $matrixUrl = "$BrokerUrl/matrix?q[][pacticipant]=$Pacticipant&q[][version]=$Version&latestby=cvp"
    
    Write-Host "Querying: $matrixUrl" -ForegroundColor DarkGray
    Write-Host ""
    
    $response = Invoke-RestMethod -Uri $matrixUrl -Headers $headers -Method Get
    
    if ($response.matrix -and $response.matrix.Count -gt 0) {
        $allVerified = $true
        $results = @()
        
        foreach ($item in $response.matrix) {
            $consumer = $item.consumer.name
            $provider = $item.provider.name
            $verificationResult = $item.verificationResult
            
            $status = "UNKNOWN"
            $color = "Yellow"
            
            if ($verificationResult) {
                if ($verificationResult.success -eq $true) {
                    $status = "VERIFIED"
                    $color = "Green"
                } else {
                    $status = "FAILED"
                    $color = "Red"
                    $allVerified = $false
                }
            } else {
                $status = "NOT VERIFIED"
                $color = "Yellow"
                $allVerified = $false
            }
            
            $results += [PSCustomObject]@{
                Consumer = $consumer
                Provider = $provider
                Status = $status
                Color = $color
            }
        }
        
        # Display results
        Write-Host "Verification Status:" -ForegroundColor White
        Write-Host ""
        
        foreach ($result in $results) {
            Write-Host "  $($result.Consumer) -> $($result.Provider): " -NoNewline
            Write-Host $result.Status -ForegroundColor $result.Color
        }
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        
        if ($allVerified) {
            Write-Host "[OK] SAFE TO DEPLOY" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "$Pacticipant v$Version can be deployed to $To" -ForegroundColor Green
            Write-Host "All contracts have been verified" -ForegroundColor Green
            exit 0
        } else {
            Write-Host "[BLOCKED] NOT SAFE TO DEPLOY" -ForegroundColor Red
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "$Pacticipant v$Version cannot be deployed to $To" -ForegroundColor Red
            Write-Host "Some contracts are not verified or have verification failures" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Next steps:" -ForegroundColor Yellow
            Write-Host "  1. Run provider verification tests" -ForegroundColor Gray
            Write-Host "  2. Fix any failing verifications" -ForegroundColor Gray
            Write-Host "  3. Publish verification results" -ForegroundColor Gray
            exit 1
        }
    } else {
        Write-Host "========================================" -ForegroundColor Yellow
        Write-Host "[WARNING] NO CONTRACTS FOUND" -ForegroundColor Yellow
        Write-Host "========================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "No pact contracts found for $Pacticipant v$Version" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "This could mean:" -ForegroundColor Gray
        Write-Host "  - No consumers depend on this provider" -ForegroundColor Gray
        Write-Host "  - Pacts haven't been published yet" -ForegroundColor Gray
        Write-Host "  - Wrong pacticipant name or version" -ForegroundColor Gray
        Write-Host ""
        Write-Host "To publish pacts:" -ForegroundColor Cyan
        Write-Host "  .\publish-pacts.ps1 -Version `"$Version`" -Branch `"main`"" -ForegroundColor Gray
        exit 1
    }
}
catch {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "[ERROR] DEPLOYMENT CHECK FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "HTTP Status: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq 401 -or $statusCode -eq 403) {
            Write-Host ""
            Write-Host "Authentication failed. Check your credentials:" -ForegroundColor Yellow
            Write-Host "  `$env:PACT_BROKER_USERNAME='admin'" -ForegroundColor Gray
            Write-Host "  `$env:PACT_BROKER_PASSWORD='admin'" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Check broker is running: .\start-pact-broker.ps1 -Action status" -ForegroundColor Gray
    Write-Host "  - Verify broker URL: $BrokerUrl" -ForegroundColor Gray
    Write-Host "  - Check authentication credentials" -ForegroundColor Gray
    exit 1
}

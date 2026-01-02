# Run All Script
Write-Host "Starting Inventory Service..." -ForegroundColor Cyan
Start-Process dotnet "run --project src/InventoryService.Api/InventoryService.Api.csproj" -NoNewWindow

Write-Host "Starting Order Service..." -ForegroundColor Cyan
Start-Process dotnet "run --project src/OrderService.Api/OrderService.Api.csproj" -NoNewWindow

Write-Host "Starting Order Web (MVC)..." -ForegroundColor Cyan
Start-Process dotnet "run --project src/OrderWeb.Mvc/OrderWeb.Mvc.csproj" -NoNewWindow

Write-Host "All services starting. Waiting 10 seconds for initialization..." -ForegroundColor Green
Start-Sleep -Seconds 10

Write-Host "Validating Health..." -ForegroundColor Yellow
Invoke-RestMethod -Uri "http://localhost:5001/api/inventory/health" | Format-Table
Invoke-RestMethod -Uri "http://localhost:5000/api/orders/health" | Format-Table

Write-Host "Ready! Access MVC App at http://localhost:5002" -ForegroundColor Green

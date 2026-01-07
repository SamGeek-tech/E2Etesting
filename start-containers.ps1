Write-Host "Waiting for Docker to be ready..."
$retries = 0
while ($retries -lt 60) {
    $error.Clear()
    try {
        docker info | Out-Null
        if ($?) {
            Write-Host "`nDocker is ready! Starting services..."
            docker-compose up --build
            return
        }
    }
    catch {
        # Ignore errors and wait
    }
    
    Start-Sleep -Seconds 2
    Write-Host -NoNewline "."
    $retries++
}
Write-Error "`nDocker did not start within 120 seconds. Please manually ensure Docker Desktop is running and try again."

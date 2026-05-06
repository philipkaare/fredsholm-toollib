param(
    [string]$Command = "up",
    [string]$AdminPassword
)

# Resolve ADMIN_PASSWORD
if (-not $AdminPassword) {
    if ($env:ADMIN_PASSWORD) {
        $AdminPassword = $env:ADMIN_PASSWORD
    } else {
        $AdminPassword = Read-Host "Indtast ADMIN_PASSWORD"
    }
}

if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    Write-Error "ADMIN_PASSWORD must not be empty."
    exit 1
}

$pwErrors = @()
if ($AdminPassword.Length -lt 8)                                  { $pwErrors += "at least 8 characters" }
if ($AdminPassword -cnotmatch '[A-Z]')                            { $pwErrors += "at least one uppercase letter" }
if ($AdminPassword -notmatch '\d')                                { $pwErrors += "at least one digit" }
if ($AdminPassword -notmatch '[^a-zA-Z0-9]')                     { $pwErrors += "at least one special character" }
if ($pwErrors.Count -gt 0) {
    Write-Error "ADMIN_PASSWORD does not meet requirements: $($pwErrors -join ', ')."
    exit 1
}

$env:ADMIN_PASSWORD = $AdminPassword

switch ($Command) {
    "up" {
        Write-Host "Bygger og starter alle services..."
        docker compose up --build -d
        if ($?) {
            Write-Host "Korer pa http://localhost:8080"
            Write-Host "Log: docker compose logs -f"
        }
    }
    "down" {
        Write-Host "Stopper og fjerner containers..."
        docker compose down
    }
    "logs" {
        docker compose logs -f
    }
    "restart" {
        Write-Host "Genstarter app-container..."
        docker compose restart app
    }
    "rebuild" {
        Write-Host "Tvinger fuld rebuild og genstarter..."
        docker compose down
        docker compose build --no-cache
        docker compose up -d
    }
    default {
        Write-Host "Ukendt kommando: $Command"
        Write-Host "Brug: .\run.ps1 [-Command up|down|logs|restart|rebuild] [-AdminPassword <pw>"
        exit 1
    }
}

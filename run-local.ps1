#!/usr/bin/env pwsh
# Einziger Startweg fuer weekplan lokal: Server und Client zusammen.
# Beenden mit Strg+C — beide Prozesse werden mitgenommen.
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$serverUrl = 'http://localhost:5080'
$clientUrl = 'http://localhost:5180'

# Erst bauen, dann starten. Zwei gleichzeitige `dotnet run` bauen dieselben
# gemeinsamen Projekte und sperren sich gegenseitig aus der obj-Ablage aus.
Write-Host 'Baue ...'
dotnet build (Join-Path $root 'Weekplan.slnx') --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Build fehlgeschlagen — es wird nichts gestartet.'
    exit $LASTEXITCODE
}

# Ohne Stammdaten hat der Server nichts auszuliefern und die App bleibt leer.
# Der Ordner entsteht aus dem Altbestand und liegt nicht im Repo, also wird er
# beim ersten Start hier angelegt.
$stammdaten = Join-Path $root 'src/Weekplan.Server/stammdaten'
if (-not (Test-Path (Join-Path $stammdaten 'liste'))) {
    Write-Host 'Befuelle die Stammdaten (einmalig) ...'
    dotnet run --no-build --project (Join-Path $root 'tools/Weekplan.Stammdaten') -- $stammdaten
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Die Stammdaten liessen sich nicht befuellen — es wird nichts gestartet.'
        exit $LASTEXITCODE
    }
}

$processes = @()
try {
    $processes += Start-Process dotnet -PassThru -NoNewWindow `
        -ArgumentList 'run', '--no-build', '--project', (Join-Path $root 'src/Weekplan.Server')
    $processes += Start-Process dotnet -PassThru -NoNewWindow `
        -ArgumentList 'run', '--no-build', '--project', (Join-Path $root 'src/Weekplan.Client')

    Write-Host ''
    Write-Host "Server  $serverUrl/health"
    Write-Host "Client  $clientUrl"
    Write-Host ''
    Write-Host 'Strg+C beendet beide.'

    while ($true) {
        if ($processes | Where-Object { $_.HasExited }) {
            Write-Host 'Ein Prozess hat sich beendet — der andere wird mitgenommen.'
            break
        }
        Start-Sleep -Seconds 1
    }
}
finally {
    foreach ($p in $processes) {
        if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    }
}

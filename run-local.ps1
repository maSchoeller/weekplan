#!/usr/bin/env pwsh
# Einziger Startweg fuer weekplan lokal: Server und Client zusammen.
# Die alte statische App laeuft unabhaengig davon mit `npx serve .`.
# Beenden mit Strg+C — beide Prozesse werden mitgenommen.
$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$serverUrl = 'http://localhost:5080'
$clientUrl = 'http://localhost:5180'

$processes = @()
try {
    $processes += Start-Process dotnet -PassThru -NoNewWindow `
        -ArgumentList 'run', '--project', (Join-Path $root 'src/Weekplan.Server')
    $processes += Start-Process dotnet -PassThru -NoNewWindow `
        -ArgumentList 'run', '--project', (Join-Path $root 'src/Weekplan.Client')

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

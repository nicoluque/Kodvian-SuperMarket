param(
  [int]$Port = 5000,
  [string]$Project = ".\KodvianSuperMarket.csproj",
  [switch]$KillExisting,
  [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

function Get-PortListeners {
  param([int]$Port)
  try {
    return Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop |
      Select-Object -ExpandProperty OwningProcess -Unique
  }
  catch {
    return @()
  }
}

function Show-ProcessInfo {
  param([int[]]$Pids)
  foreach ($procId in $Pids) {
    try {
      $p = Get-Process -Id $procId -ErrorAction Stop
      Write-Host "- PID $($p.Id) | $($p.ProcessName)" -ForegroundColor Yellow
    }
    catch {
      Write-Host "- PID $procId | (proceso no disponible)" -ForegroundColor Yellow
    }
  }
}

$listeners = Get-PortListeners -Port $Port

if ($listeners.Count -gt 0) {
  Write-Host "Puerto $Port en uso por:" -ForegroundColor Yellow
  Show-ProcessInfo -Pids $listeners

  if ($KillExisting) {
    foreach ($procId in $listeners) {
      try {
        Stop-Process -Id $procId -Force -ErrorAction Stop
        Write-Host "Proceso $procId detenido." -ForegroundColor Green
      }
      catch {
        Write-Host "No se pudo detener PID ${procId}: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
      }
    }
  }
  else {
    Write-Host "Usa -KillExisting para cerrar el proceso y continuar." -ForegroundColor Cyan
    Write-Host "Ejemplo: powershell -ExecutionPolicy Bypass -File .\scripts\start_backend_clean.ps1 -KillExisting" -ForegroundColor Cyan
    exit 1
  }
}

if (-not $SkipRestore) {
  Write-Host "Restaurando dependencias..." -ForegroundColor Cyan
  dotnet restore $Project
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$url = "http://localhost:$Port"
Write-Host "Iniciando backend en $url" -ForegroundColor Cyan
dotnet run --project $Project --no-launch-profile --urls $url
exit $LASTEXITCODE

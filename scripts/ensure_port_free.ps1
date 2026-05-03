param(
  [Parameter(Mandatory = $true)]
  [int]$Port,
  [string]$AllowedProcessNamesCsv = ""
)

$ErrorActionPreference = "Stop"

try {
  $listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique
}
catch {
  $listeners = @()
}

if (-not $listeners -or $listeners.Count -eq 0) {
  exit 0
}

foreach ($owningPid in $listeners) {
  $proc = Get-Process -Id $owningPid -ErrorAction SilentlyContinue
  if (-not $proc) {
    continue
  }

  if ($AllowedProcessNames.Count -gt 0 -and ($AllowedProcessNames -notcontains $proc.ProcessName)) {
    Write-Error "Port $Port is in use by PID $owningPid ($($proc.ProcessName)). Stop it or change debug port."
    exit 1
  }

  try {
    Stop-Process -Id $owningPid -Force -ErrorAction Stop
    Write-Host "Stopped PID $owningPid on port $Port"
  }
  catch {
    $stillAlive = Get-Process -Id $owningPid -ErrorAction SilentlyContinue
    if ($stillAlive) {
      Write-Error "Could not stop PID $owningPid ($($proc.ProcessName)) on port $Port"
      exit 1
    }
  }
}

exit 0
 $AllowedProcessNames = @()
 if (-not [string]::IsNullOrWhiteSpace($AllowedProcessNamesCsv)) {
   $AllowedProcessNames = $AllowedProcessNamesCsv.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
 }

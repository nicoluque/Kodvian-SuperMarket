param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$Project = ".\KodvianSuperMarket.csproj",
  [switch]$StartBackend,
  [string]$ConnectionString = "Host=localhost;Port=5432;Database=KodvianSuperMarket;Username=postgres;Password=1234"
)

$ErrorActionPreference = "Stop"

function Step($name, [scriptblock]$action) {
  Write-Host "`n==> $name" -ForegroundColor Cyan
  & $action
  Write-Host "OK: $name" -ForegroundColor Green
}

function ApiGet($url, $headers = @{}) {
  return Invoke-RestMethod -Method Get -Uri $url -Headers $headers
}

function ApiPostJson($url, $body, $headers = @{}) {
  return Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body ($body | ConvertTo-Json -Depth 10) -ContentType "application/json"
}

function PostExpectStatus($url, $body, [int[]]$expectedStatuses, $headers = @{}) {
  try {
    $res = Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body ($body | ConvertTo-Json -Depth 10) -ContentType "application/json"
    if (-not ($expectedStatuses -contains 200)) {
      throw "Expected statuses $($expectedStatuses -join ',') but got 200"
    }
    return @{ status = 200; body = $res }
  }
  catch {
    if (-not $_.Exception.Response) { throw }
    $status = $_.Exception.Response.StatusCode.value__
    if ($expectedStatuses -contains $status) {
      return @{ status = $status; body = $null }
    }
    throw "Expected statuses $($expectedStatuses -join ',') but got $status"
  }
}

function Get-BasePort([string]$url) {
  $uri = [System.Uri]$url
  return $uri.Port
}

function Is-PortListening([int]$port) {
  try {
    $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction Stop
    return $listeners.Count -gt 0
  }
  catch {
    return $false
  }
}

function Is-ApiHealthy([string]$url) {
  try {
    $health = ApiGet "$url/api/v1/health"
    return $health.status -eq "ok"
  }
  catch {
    return $false
  }
}

$backend = $null
$startedByScript = $false

try {
  if ($StartBackend) {
    Step "Start backend" {
      $port = Get-BasePort $BaseUrl
      if (Is-PortListening $port) {
        if (Is-ApiHealthy $BaseUrl) {
          Write-Host "Backend ya activo en $BaseUrl. Se reutiliza instancia existente." -ForegroundColor Yellow
        }
        else {
          throw "El puerto $port ya esta en uso y el health check no responde. Cierra el proceso que ocupa el puerto o usa otro BaseUrl."
        }
      }
      else {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:ConnectionStrings__DefaultConnection = $ConnectionString
        $backend = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$Project`" --no-build --no-launch-profile --urls `"$BaseUrl`"" -PassThru -WindowStyle Hidden
        $startedByScript = $true
        Start-Sleep -Seconds 10
      }
    }
  }

  Step "Health check" {
    $health = ApiGet "$BaseUrl/api/v1/health"
    if ($health.status -ne "ok") {
      throw "Health status is not ok"
    }
  }

  Step "Reset demo data" {
    $null = ApiPostJson "$BaseUrl/api/v1/admin/demo/reset" @{}
  }

  Step "Verify demo status" {
    $status = ApiGet "$BaseUrl/api/v1/admin/demo/status"
    if (-not $status.exists) { throw "Demo tenant was not created" }
    if ([int]$status.users -lt 4) { throw "Expected at least 4 demo users" }
    Write-Host "Demo users: $($status.users) | storeId: $($status.storeId)"
  }

  $users = @(
    @{ username = "demo.admin"; password = "demo123"; pin = "1111"; role = "Admin" },
    @{ username = "demo.encargado"; password = "demo123"; pin = "2222"; role = "Supervisor" },
    @{ username = "demo.caja"; password = "demo123"; pin = "3333"; role = "Operator" },
    @{ username = "demo.tablet"; password = "demo123"; pin = "4444"; role = "Operator" }
  )

  Step "Validate /auth/login for all demo users" {
    foreach ($u in $users) {
      $res = PostExpectStatus "$BaseUrl/api/v1/auth/login" @{ username = $u.username; password = $u.password; pin = $u.pin } @(200)
      if ($res.body.role -ne $u.role) {
        throw "Unexpected role for $($u.username). Expected $($u.role), got $($res.body.role)"
      }
      Write-Host "LOGIN $($u.username) => 200 ($($res.body.role))" -ForegroundColor DarkGreen
    }
  }

  Step "Validate /auth/bo-login role access" {
    foreach ($u in $users) {
      $allowed = $u.username -in @("demo.admin", "demo.encargado")
      $expected = if ($allowed) { @(200) } else { @(403) }
      $res = PostExpectStatus "$BaseUrl/api/v1/auth/bo-login" @{ username = $u.username; password = $u.password; pin = $u.pin } $expected
      Write-Host "BO_LOGIN $($u.username) => $($res.status)" -ForegroundColor DarkGreen
    }
  }

  Step "Validate POS device tokens" {
    $caja = ApiGet "$BaseUrl/api/v1/auth/device/validate" @{ "X-Device-Token" = "demo-device-caja" }
    $tablet = ApiGet "$BaseUrl/api/v1/auth/device/validate" @{ "X-Device-Token" = "demo-device-tablet" }
    if ($caja.deviceType -ne "CashRegister") { throw "demo-device-caja should be CashRegister" }
    if ($tablet.deviceType -ne "Tablet") { throw "demo-device-tablet should be Tablet" }
  }

  Step "Validate key POS operator sessions" {
    $cajaSession = PostExpectStatus "$BaseUrl/api/v1/auth/operator-session" @{ username = "demo.caja"; password = "demo123"; pin = "3333" } @(200) @{ "X-Device-Token" = "demo-device-caja" }
    if (-not $cajaSession.body.sessionToken) { throw "No session token for demo.caja on caja token" }

    $tabletSession = PostExpectStatus "$BaseUrl/api/v1/auth/operator-session" @{ username = "demo.tablet"; password = "demo123"; pin = "4444" } @(200) @{ "X-Device-Token" = "demo-device-tablet" }
    if (-not $tabletSession.body.sessionToken) { throw "No session token for demo.tablet on tablet token" }
  }

  Write-Host "`nDemo environment is stable and verified." -ForegroundColor Green
}
finally {
  if ($startedByScript -and $backend) {
    Stop-Process -Id $backend.Id -Force -ErrorAction SilentlyContinue
  }
}

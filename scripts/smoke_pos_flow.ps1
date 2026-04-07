param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$DeviceToken = "demo-device-caja",
  [string]$Username = "demo.caja",
  [string]$Password = "demo123",
  [string]$Pin = "3333",
  [string]$OperatingMode = ""
)

$ErrorActionPreference = "Stop"

function Step($name, [scriptblock]$action) {
  Write-Host "`n==> $name" -ForegroundColor Cyan
  & $action
  Write-Host "OK: $name" -ForegroundColor Green
}

function ApiPostJson($url, $body, $headers = @{}) {
  return Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body ($body | ConvertTo-Json -Depth 8) -ContentType "application/json"
}

function ApiGet($url, $headers = @{}) {
  return Invoke-RestMethod -Method Get -Uri $url -Headers $headers
}

function ResolveModulesForMode($mode) {
  switch ($mode) {
    "MostradorExpress" {
      return @{
        tablet = $false
        envases = $false
        cuentaCorriente = $false
        comprasSugeridas = $true
        reportes = $true
      }
    }
    "CajaRapida" {
      return @{
        tablet = $false
        envases = $false
        cuentaCorriente = $false
        comprasSugeridas = $false
        reportes = $true
      }
    }
    default {
      return @{
        tablet = $true
        envases = $true
        cuentaCorriente = $true
        comprasSugeridas = $true
        reportes = $true
      }
    }
  }
}

Write-Host "Running POS smoke flow against $BaseUrl" -ForegroundColor Yellow

Step "Health check" {
  $health = ApiGet "$BaseUrl/api/v1/health"
  if ($health.status -ne "ok") {
    throw "Health status is not ok"
  }
}

Step "Demo reset" {
  $null = ApiPostJson "$BaseUrl/api/v1/admin/demo/reset" @{}
}

if ($OperatingMode -ne "") {
  Step "Configurar modo operativo en demo" {
    if ($OperatingMode -ne "MiniMarketFull" -and $OperatingMode -ne "MostradorExpress" -and $OperatingMode -ne "CajaRapida") {
      throw "OperatingMode invalido: $OperatingMode"
    }

    $status = ApiGet "$BaseUrl/api/v1/admin/demo/status"
    if (-not $status.storeId) {
      throw "No se pudo resolver storeId de demo"
    }

    $storeId = [int]$status.storeId
    $auth = ApiPostJson "$BaseUrl/api/v1/auth/operator-session" @{
      username = "demo.encargado"
      password = "demo123"
      pin = "2222"
    } @{ "X-Device-Token" = $DeviceToken }
    if (-not $auth.sessionToken) {
      throw "No se pudo obtener sesion para configurar modo"
    }

    $adminHeaders = @{ "X-Operator-Session" = $auth.sessionToken; "X-Device-Token" = $DeviceToken }
    $current = ApiGet "$BaseUrl/api/v1/stores/$storeId/settings" $adminHeaders
    $payload = @{}
    if ($null -ne $current) {
      foreach ($prop in $current.PSObject.Properties) {
        $payload[$prop.Name] = $prop.Value
      }
    }
    $payload["operatingMode"] = $OperatingMode
    $payload["enabledModules"] = ResolveModulesForMode $OperatingMode
    $null = Invoke-RestMethod -Method Put -Uri "$BaseUrl/api/v1/stores/$storeId/settings" -Headers $adminHeaders -Body ($payload | ConvertTo-Json -Depth 8) -ContentType "application/json"
  }
}

Step "Invalid token must be 401" {
  try {
    $null = Invoke-WebRequest -Method Get -Uri "$BaseUrl/api/v1/auth/device/validate" -Headers @{ "X-Device-Token" = "invalid-token" }
    throw "Expected 401 for invalid token"
  }
  catch {
    if (-not $_.Exception.Response -or $_.Exception.Response.StatusCode.value__ -ne 401) {
      throw "Expected 401 for invalid token, got different response"
    }
  }
}

Step "Device token validate" {
  $valid = ApiGet "$BaseUrl/api/v1/auth/device/validate" @{ "X-Device-Token" = $DeviceToken }
  if (-not $valid.valid) {
    throw "Device validation returned invalid"
  }
  if ($OperatingMode -ne "" -and $valid.operatingMode -ne $OperatingMode) {
    throw "El modo retornado por device/validate no coincide: esperado=$OperatingMode actual=$($valid.operatingMode)"
  }
}

$sessionToken = $null

Step "Operator session create" {
  $session = ApiPostJson "$BaseUrl/api/v1/auth/operator-session" @{
    username = $Username
    password = $Password
    pin = $Pin
  } @{ "X-Device-Token" = $DeviceToken }
  if (-not $session.sessionToken) {
    throw "No session token in response"
  }
  $script:sessionToken = $session.sessionToken
}

$headers = @{ "X-Device-Token" = $DeviceToken; "X-Operator-Session" = $sessionToken }

Step "Open or get current cash session" {
  try {
    $opened = ApiPostJson "$BaseUrl/api/v1/cash-sessions/open" @{ shift = "Morning"; openingCash = 10000 } $headers
    Write-Host "Opened cash session #$($opened.id)"
  }
  catch {
    Write-Host "Open returned non-success, checking current session..." -ForegroundColor DarkYellow
  }

  $current = ApiGet "$BaseUrl/api/v1/cash-sessions/current" $headers
  if (-not $current.id) {
    throw "Current cash session not found"
  }
}

Step "Cashier inbox" {
  $inbox = ApiGet "$BaseUrl/api/v1/cashier/inbox" $headers
  if ($null -eq $inbox) {
    throw "Inbox endpoint returned null"
  }
}

Write-Host "`nPOS smoke flow completed successfully." -ForegroundColor Green

if ($OperatingMode -ne "") {
  Write-Host "Modo validado: $OperatingMode" -ForegroundColor Green
}

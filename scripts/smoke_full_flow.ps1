param(
  [string]$BaseUrl = "http://localhost:5000",
  [string]$DeviceToken = "demo-device-caja",
  [string]$Username = "demo.caja",
  [string]$Password = "demo123",
  [string]$Pin = "3333",
  [string]$ProductBarcode = "779000000001"
)

$ErrorActionPreference = "Stop"

function Step($name, [scriptblock]$action) {
  Write-Host "`n==> $name" -ForegroundColor Cyan
  & $action
  Write-Host "OK: $name" -ForegroundColor Green
}

function ToJson($obj) {
  return ($obj | ConvertTo-Json -Depth 10)
}

function ApiGet($url, $headers = @{}) {
  return Invoke-RestMethod -Method Get -Uri $url -Headers $headers
}

function ApiPostJson($url, $body, $headers = @{}) {
  return Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body (ToJson $body) -ContentType "application/json"
}

function WebGet($url, $headers = @{}) {
  return Invoke-WebRequest -Method Get -Uri $url -Headers $headers -UseBasicParsing
}

function WebPostJson($url, $body, $headers = @{}) {
  return Invoke-WebRequest -Method Post -Uri $url -Headers $headers -Body (ToJson $body) -ContentType "application/json" -UseBasicParsing
}

function ParseJsonOrNull([string]$text) {
  try { return ($text | ConvertFrom-Json) } catch { return $null }
}

Write-Host "Running FULL smoke flow against $BaseUrl" -ForegroundColor Yellow

Step "Health check" {
  $health = ApiGet "$BaseUrl/api/v1/health"
  if ($health.status -ne "ok") { throw "Health status is not ok" }
}

Step "Demo reset" {
  $null = ApiPostJson "$BaseUrl/api/v1/admin/demo/reset" @{}
}

Step "Invalid device token must be 401" {
  try {
    $null = WebGet "$BaseUrl/api/v1/auth/device/validate" @{ "X-Device-Token" = "invalid-token" }
    throw "Expected 401 for invalid token"
  }
  catch {
    if (-not $_.Exception.Response -or $_.Exception.Response.StatusCode.value__ -ne 401) {
      throw "Expected 401 for invalid token"
    }
  }
}

Step "Validate demo device token" {
  $valid = ApiGet "$BaseUrl/api/v1/auth/device/validate" @{ "X-Device-Token" = $DeviceToken }
  if (-not $valid.valid) { throw "Device token is not valid" }
}

$sessionToken = $null
Step "Create operator session" {
  $session = ApiPostJson "$BaseUrl/api/v1/auth/operator-session" @{ username = $Username; password = $Password; pin = $Pin } @{ "X-Device-Token" = $DeviceToken }
  if (-not $session.sessionToken) { throw "No sessionToken returned" }
  $script:sessionToken = $session.sessionToken
}

$authHeaders = @{ "X-Device-Token" = $DeviceToken; "X-Operator-Session" = $sessionToken }
$currentSession = $null

Step "Open cash session or use current" {
  try {
    $opened = ApiPostJson "$BaseUrl/api/v1/cash-sessions/open" @{ shift = "Morning"; openingCash = 10000 } $authHeaders
    Write-Host "Opened cash session #$($opened.id)"
  }
  catch {
    Write-Host "Open returned non-success, trying current" -ForegroundColor DarkYellow
  }

  $current = ApiGet "$BaseUrl/api/v1/cash-sessions/current" $authHeaders
  if (-not $current.id) { throw "No current cash session" }
  $script:currentSession = $current
}

$cart = $null
Step "Create cart" {
  $created = ApiPostJson "$BaseUrl/api/v1/carts" @{} @{ "X-Device-Token" = $DeviceToken }
  if (-not $created.id) { throw "Cart was not created" }
  $script:cart = $created
}

Step "Add item to cart" {
  $item = ApiPostJson "$BaseUrl/api/v1/carts/$($cart.id)/items" @{
    productCode = $ProductBarcode
    productName = "auto"
    unitPrice = 0
    quantity = 1
    unit = "Unit"
    discount = 0
  } @{ "X-Device-Token" = $DeviceToken }

  if (-not $item.id) { throw "Cart item was not created" }
}

$cartState = $null
Step "Get cart and total" {
  $state = ApiGet "$BaseUrl/api/v1/carts/$($cart.id)" @{ "X-Device-Token" = $DeviceToken }
  if (-not $state.total -or [decimal]$state.total -le 0) { throw "Cart total is invalid" }
  $script:cartState = $state
}

Step "Send cart to cashier" {
  $sent = ApiPostJson "$BaseUrl/api/v1/carts/$($cart.id)/send-to-cashier" @{} $authHeaders
  if (-not $sent.id) { throw "Cart was not sent to cashier" }
}

$sale = $null
Step "Create sale from cart" {
  try {
    $createdSale = ApiPostJson "$BaseUrl/api/v1/sales/from-cart/$($cart.id)" @{
      customerId = $null
      discount = 0
      payments = @(@{ paymentMethod = "Cash"; amount = [decimal]$cartState.total; reference = "SMOKE"; isPending = $false })
    } $authHeaders
  }
  catch {
    if ($_.Exception.Response) {
      $stream = $_.Exception.Response.GetResponseStream()
      $reader = New-Object System.IO.StreamReader($stream)
      $body = $reader.ReadToEnd()
      Write-Host "Sale creation failed payload: $body" -ForegroundColor Yellow
    }
    throw
  }

  if (-not $createdSale.id) { throw "Sale was not created" }
  $script:sale = $createdSale
}

Step "Fetch sale by id" {
  $loaded = ApiGet "$BaseUrl/api/v1/sales/$($sale.id)" $authHeaders
  if (-not $loaded.id) { throw "Sale get failed" }
}

Step "Print endpoint for sale" {
  $print = ApiGet "$BaseUrl/api/v1/print/sales/$($sale.id)" @{ "X-Store-Id" = "$($currentSession.storeId)" }
  if (-not $print.saleId -and -not $print.id) {
    throw "Sale print payload invalid"
  }
}

Step "Exports daily sales endpoint" {
  $exportResp = WebGet "$BaseUrl/api/v1/exports/sales/daily?format=xlsx"
  if ($exportResp.StatusCode -ne 200) { throw "Export endpoint failed" }
  if (-not $exportResp.Headers["Content-Type"] -or $exportResp.Headers["Content-Type"] -notlike "*spreadsheetml*") {
    throw "Unexpected export content-type: $($exportResp.Headers['Content-Type'])"
  }
}

Step "Attempt cash close (accept blocked-by-tasks as valid behavior)" {
  try {
    $closeResp = WebPostJson "$BaseUrl/api/v1/cash-sessions/$($currentSession.id)/close" @{
      declaredCash = [decimal]$currentSession.totalCash
      declaredCard = [decimal]$currentSession.totalCard
      declaredTransfer = [decimal]$currentSession.totalTransfer
      declaredCredit = [decimal]$currentSession.totalCredit
      notes = "Smoke full flow"
    } $authHeaders

    if ($closeResp.StatusCode -ne 200) {
      throw "Close returned unexpected status $($closeResp.StatusCode)"
    }
  }
  catch {
    if (-not $_.Exception.Response) { throw }
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -ne 400 -and $status -ne 409) { throw }

    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $body = $reader.ReadToEnd()
    $payload = ParseJsonOrNull $body
    if (-not $payload) { throw "Close failed with non-json payload" }

    $msg = "$($payload.message)"
    if ($msg -notlike "*required tasks*" -and $msg -notlike "*pending*") {
      throw "Close failed for an unexpected business reason: $msg"
    }

    Write-Host "Close blocked by required tasks (expected in some flows)" -ForegroundColor DarkYellow
  }
}

Write-Host "`nFULL smoke flow completed successfully." -ForegroundColor Green

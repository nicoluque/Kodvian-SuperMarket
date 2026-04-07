using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/sales")]
[DeviceAuth]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IOperatorSessionService _operatorSessionService;
    private readonly IAuditService _auditService;
    private readonly ICashSessionService _cashSessionService;
    private readonly ICustomerService _customerService;
    private readonly IRequestDeduplicationService _requestDeduplication;
    private readonly IConfiguration _config;
    private readonly IStoreShiftService _storeShiftService;

    public SalesController(ISaleService saleService, IOperatorSessionService operatorSessionService, IAuditService auditService, ICashSessionService cashSessionService, ICustomerService customerService, IRequestDeduplicationService requestDeduplication, IConfiguration config, IStoreShiftService storeShiftService)
    {
        _saleService = saleService;
        _operatorSessionService = operatorSessionService;
        _auditService = auditService;
        _cashSessionService = cashSessionService;
        _customerService = customerService;
        _requestDeduplication = requestDeduplication;
        _config = config;
        _storeShiftService = storeShiftService;
    }

    [HttpPost("from-cart/{cartId}")]
    [OperatorSessionAuth]
    public async Task<ActionResult<SaleResponse>> CreateFromCart(int cartId, [FromBody] SaleCreateRequest request)
    {
        var sessionId = (int)HttpContext.Items["SessionId"]!;
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        var isTotemQrOnly = device?.StoreId != null && await _storeShiftService.IsTotemQrOnlyStoreAsync(device.StoreId.Value);

        if (!request.Payments.Any())
            return BadRequest(new { message = "Debes informar al menos un medio de pago" });

        CashSession? cashSession = null;

        if (device?.DeviceType == "CashRegister")
        {
            cashSession = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
        }
        else if (device?.DeviceType == "Tablet")
        {
            cashSession = await _cashSessionService.GetCurrentForTabletAsync(deviceId);
        }

        var allowQueueWithoutCashSession = device?.DeviceType == "Tablet";
        if (cashSession == null && !isTotemQrOnly && !allowQueueWithoutCashSession)
            return BadRequest(new { message = "No hay una caja abierta. Abre una caja para continuar." });

        using var dedup = _requestDeduplication.Acquire($"sales/from-cart/{cartId}");
        var payments = request.Payments.Select(p => (p.PaymentMethod, p.Amount, p.Reference, p.IsPending)).ToList();
        var hasPendingTransfer = request.Payments.Any(p => p.PaymentMethod == PaymentMethod.Transfer.ToString() && p.IsPending);
        var hasAccountCredit = request.Payments.Any(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString());

        if (hasPendingTransfer && request.CustomerId == null)
            return BadRequest(new { message = "Debes seleccionar cliente cuando hay una transferencia pendiente" });

        if (hasAccountCredit && request.AccountCreditCustomerId == null)
            return BadRequest(new { message = "Debes seleccionar cliente para usar cuenta corriente" });

        if (request.CustomerId.HasValue && request.AccountCreditCustomerId.HasValue && request.CustomerId.Value != request.AccountCreditCustomerId.Value)
            return BadRequest(new { message = "El cliente del cobro y el de cuenta corriente deben coincidir" });

        var effectiveCustomerId = request.CustomerId ?? request.AccountCreditCustomerId;

        if (hasAccountCredit && request.AccountCreditCustomerId.HasValue)
        {
            var accountCreditAmount = request.Payments.First(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString()).Amount;
            var canFiado = await _customerService.CanCreateFiadoAsync(request.AccountCreditCustomerId.Value, accountCreditAmount);
            if (!canFiado)
                return BadRequest(new { message = "El cliente no puede operar en cuenta corriente: está deshabilitado, en estado pendiente, con deuda vencida o supera el 110% del límite" });
        }

        var sale = await _saleService.CreateFromCartAsync(cartId, sessionId, deviceId, cashSession?.Id, effectiveCustomerId, request.Discount, payments);

        if (isTotemQrOnly && sale.StoreId.HasValue)
        {
            var decision = await _storeShiftService.ResolveSaleAssignmentAsync(sale.StoreId.Value, sale.CreatedAt);
            sale.ShiftAssignmentStatus = decision.AssignmentStatus;
            sale.ShiftBucket = decision.ShiftBucket;
            sale.ExpectedShiftBucket = decision.ExpectedShiftBucket;
            sale.ShiftAssignedAt = decision.AssignmentStatus == "Assigned" ? DateTime.UtcNow : null;
            sale.ShiftAssignedByUsuarioId = decision.AssignmentStatus == "Assigned" ? sessionUsuarioId : null;
            sale.ShiftAssignmentReason = decision.Reason;
            await dbContext.SaveChangesAsync();
        }

        if (hasAccountCredit && request.AccountCreditCustomerId.HasValue)
        {
            if (cashSession != null)
            {
                var accountCreditPayment = request.Payments.First(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString());
                await _customerService.ProcessAccountCreditPaymentAsync(sale, request.AccountCreditCustomerId.Value, accountCreditPayment.Amount);
            }
        }

        await _auditService.LogAsync(
            AuditEventType.Other,
            sessionUsuarioId,
            $"Sale created from cart {cartId}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers.UserAgent.ToString(),
            null,
            $"SaleId: {sale.Id}, Total: {sale.Total}, Status: {sale.Status}",
            true
        );

        return Ok(ToSaleResponse(sale));
    }

    [HttpPost]
    [OperatorSessionAuth]
    public async Task<ActionResult<SaleResponse>> Create([FromBody] SaleCreateRequest request)
    {
        var sessionId = (int)HttpContext.Items["SessionId"]!;
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        if (!request.Payments.Any())
            return BadRequest(new { message = "Debes informar al menos un medio de pago" });

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        var isTotemQrOnly = device?.StoreId != null && await _storeShiftService.IsTotemQrOnlyStoreAsync(device.StoreId.Value);

        CashSession? cashSession = null;

        if (device?.DeviceType == "CashRegister")
        {
            cashSession = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
        }
        else if (device?.DeviceType == "Tablet")
        {
            cashSession = await _cashSessionService.GetCurrentForTabletAsync(deviceId);
        }

        if (cashSession == null && !isTotemQrOnly)
            return BadRequest(new { message = "No hay una caja abierta. Abre una caja para continuar." });

        var hasPendingTransfer = request.Payments.Any(p => p.PaymentMethod == PaymentMethod.Transfer.ToString() && p.IsPending);
        var useWebhookOnly = !bool.TryParse(_config["MercadoPago:MercadoPagoUseWebhookConfirmationOnly"], out var configured) || configured;
        var hasPendingQr = useWebhookOnly && request.Payments.Any(p => p.PaymentMethod == PaymentMethod.QrMp.ToString());
        var hasAccountCredit = request.Payments.Any(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString());

        if (hasPendingTransfer && request.CustomerId == null)
            return BadRequest(new { message = "Debes seleccionar cliente cuando hay una transferencia pendiente" });

        if (hasAccountCredit && request.AccountCreditCustomerId == null)
            return BadRequest(new { message = "Debes seleccionar cliente para usar cuenta corriente" });

        if (hasAccountCredit && request.AccountCreditCustomerId.HasValue)
        {
            var accountCreditAmount = request.Payments.First(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString()).Amount;
            var canFiado = await _customerService.CanCreateFiadoAsync(request.AccountCreditCustomerId.Value, accountCreditAmount);
            if (!canFiado)
                return BadRequest(new { message = "El cliente no puede operar en cuenta corriente: está deshabilitado, en estado pendiente, con deuda vencida o supera el 110% del límite" });
        }

        if (request.CustomerId.HasValue)
        {
            var existingPendingTransfer = await dbContext.Sales
                .FirstOrDefaultAsync(s => s.CustomerId == request.CustomerId.Value && s.Status == SaleStatus.PendingTransfer.ToString());

            if (existingPendingTransfer != null)
                return Conflict(new { message = $"El cliente ya tiene una transferencia pendiente (Venta #{existingPendingTransfer.Id}). No se puede crear otra." });
                
        }

        var payments = request.Payments.Select(p => (p.PaymentMethod, p.Amount, p.Reference, p.IsPending)).ToList();
        
        var subtotal = 0m;
        
        string? invoiceNumber = null;
        DateTime? completedAt = null;
        var saleStatus = SaleStatus.Completed.ToString();

        if (hasPendingTransfer)
        {
            saleStatus = SaleStatus.PendingTransfer.ToString();
        }
        else if (hasPendingQr)
        {
            saleStatus = SaleStatus.Pending.ToString();
        }
        else
        {
            invoiceNumber = await _saleService.GenerateInvoiceNumber();
            completedAt = DateTime.UtcNow;
        }

        var sale = new Sale
        {
            DeviceId = deviceId,
            OperatorSessionId = sessionId,
            CashSessionId = cashSession?.Id,
            CustomerId = request.CustomerId,
            Status = saleStatus,
            Subtotal = subtotal,
            Discount = request.Discount,
            Tax = 0,
            Total = subtotal - request.Discount,
            InvoiceNumber = invoiceNumber,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = completedAt
        };

        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();

        if (isTotemQrOnly && sale.StoreId.HasValue)
        {
            var decision = await _storeShiftService.ResolveSaleAssignmentAsync(sale.StoreId.Value, sale.CreatedAt);
            sale.ShiftAssignmentStatus = decision.AssignmentStatus;
            sale.ShiftBucket = decision.ShiftBucket;
            sale.ExpectedShiftBucket = decision.ExpectedShiftBucket;
            sale.ShiftAssignedAt = decision.AssignmentStatus == "Assigned" ? DateTime.UtcNow : null;
            sale.ShiftAssignedByUsuarioId = decision.AssignmentStatus == "Assigned" ? sessionUsuarioId : null;
            sale.ShiftAssignmentReason = decision.Reason;
            await dbContext.SaveChangesAsync();
        }

        foreach (var payment in request.Payments)
        {
            var paymentStatus = PaymentStatus.Confirmed.ToString();
            if ((payment.PaymentMethod == PaymentMethod.Transfer.ToString() && payment.IsPending)
                || (hasPendingQr && payment.PaymentMethod == PaymentMethod.QrMp.ToString()))
            {
                paymentStatus = PaymentStatus.Pending.ToString();
            }

            var salePayment = new SalePayment
            {
                SaleId = sale.Id,
                PaymentMethod = payment.PaymentMethod,
                Status = paymentStatus,
                Amount = payment.Amount,
                Reference = payment.Reference,
                Provider = payment.PaymentMethod == PaymentMethod.QrMp.ToString() ? "MercadoPago" : null,
                ExternalReference = payment.PaymentMethod == PaymentMethod.QrMp.ToString() ? sale.Id.ToString() : null,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.SalePayments.Add(salePayment);
        }

        if (!hasPendingTransfer && !hasPendingQr)
        {
            var paymentTotal = request.Payments.Sum(p => p.Amount);
            if (paymentTotal < sale.Total)
                return BadRequest(new { message = "Payment total is less than sale total" });

            if (cashSession?.Id != null)
                await _cashSessionService.RecalculateTotalsAsync(cashSession.Id);
        }

        if (hasAccountCredit && request.AccountCreditCustomerId.HasValue)
        {
            var accountCreditPayment = request.Payments.First(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString());
            await _customerService.ProcessAccountCreditPaymentAsync(sale, request.AccountCreditCustomerId.Value, accountCreditPayment.Amount);
        }

        await _auditService.LogAsync(
            AuditEventType.Other,
            sessionUsuarioId,
            $"Direct sale created",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers.UserAgent.ToString(),
            null,
            $"SaleId: {sale.Id}, Status: {sale.Status}",
            true
        );

        var createdSale = await _saleService.GetByIdAsync(sale.Id);
        return Ok(ToSaleResponse(createdSale!));
    }

    [HttpGet("pending-transfers")]
    [OperatorSessionAuth]
    public async Task<ActionResult<List<PendingTransferResponse>>> GetPendingTransfers([FromQuery] int? olderThanHours = null, [FromQuery] string scope = "device")
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden consultar transferencias pendientes" });

        var normalizedScope = (scope ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedScope.Length == 0) normalizedScope = "device";
        if (normalizedScope != "device" && normalizedScope != "current-session")
            return BadRequest(new { message = "Scope inválido. Usa 'device' o 'current-session'" });

        int? cashSessionId = null;
        if (normalizedScope == "current-session")
        {
            var currentSession = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
            if (currentSession == null)
                return Ok(new List<PendingTransferResponse>());
            cashSessionId = currentSession.Id;
        }

        var sales = await _saleService.GetPendingTransfersAsync(deviceId, cashSessionId);

        if (olderThanHours.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddHours(-olderThanHours.Value);
            sales = sales.Where(s => s.CreatedAt <= cutoff).ToList();
        }

        var response = sales.Select(s => new PendingTransferResponse
        {
            SaleId = s.Id,
            CustomerId = s.CustomerId,
            Total = s.Total,
            CreatedAt = s.CreatedAt,
            Payments = s.Payments.Select(p => new PendingTransferPaymentResponse
            {
                Id = p.Id,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status,
                Amount = p.Amount,
                Reference = p.Reference
            }).ToList()
        }).ToList();

        return Ok(response);
    }

    [HttpGet("pending-transfers/alerts")]
    [OperatorSessionAuth]
    public async Task<ActionResult> GetPendingTransferAlerts([FromQuery] int olderThanHours = 24)
    {
        var sales = await _saleService.GetPendingTransfersAsync();
        var cutoff = DateTime.UtcNow.AddHours(-olderThanHours);
        var older = sales.Where(s => s.CreatedAt <= cutoff).ToList();

        return Ok(new
        {
            olderThanHours,
            totalPendingTransfers = sales.Count,
            alertCount = older.Count,
            alertSaleIds = older.Select(s => s.Id).ToList()
        });
    }

    [HttpGet("return-eligible")]
    [OperatorSessionAuth]
    public async Task<ActionResult<List<ReturnEligibleSaleResponse>>> GetReturnEligibleSales([FromQuery] int hours = 48, [FromQuery] string? q = null)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        var sales = await _saleService.GetReturnEligibleSalesAsync(device?.StoreId, hours, q);

        var response = sales.Select(s => new ReturnEligibleSaleResponse
        {
            Id = s.Id,
            CreatedAt = s.CreatedAt,
            Total = s.Total,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer?.FullName,
            Status = s.Status
        }).ToList();

        return Ok(response);
    }

    [HttpPost("{id}/confirm-transfer")]
    [OperatorSessionAuth]
    public async Task<ActionResult<SaleResponse>> ConfirmTransfer(int id, [FromBody] ConfirmTransferRequest request)
    {
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden confirmar transferencias" });

        var usuario = await dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == sessionUsuarioId && u.IsActive);
        if (usuario == null)
            return Unauthorized(new { message = "Sesion de operador invalida" });

        if (!CanOperatePendingTransfers(usuario.Role))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permisos para confirmar transferencias" });

        using var dedup = _requestDeduplication.Acquire($"sales/{id}/confirm-transfer");
        var sale = await _saleService.ConfirmTransferAsync(id, request.PaymentId, request.Reference, request.Notes);

            await _auditService.LogAsync(
                AuditEventType.Other,
                sessionUsuarioId,
                $"Transfer confirmed for sale {id}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString(),
                null,
                $"SaleId: {sale.Id}, PaymentId: {request.PaymentId}, NewStatus: {sale.Status}",
                true
            );

            if (sale.Status == SaleStatus.Paid.ToString())
            {
                if (sale.CashSessionId.HasValue)
                    await _cashSessionService.RecalculateTotalsAsync(sale.CashSessionId.Value);
            }

        return Ok(ToSaleResponse(sale));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SaleResponse>> Get(int id)
    {
        var sale = await _saleService.GetByIdAsync(id);

        if (sale == null)
            return NotFound();

        return Ok(ToSaleResponse(sale));
    }

    [HttpPost("{id}/cancel-pending-transfer")]
    [OperatorSessionAuth]
    public async Task<ActionResult<SaleResponse>> CancelPendingTransfer(int id, [FromBody] CancelPendingTransferRequest request)
    {
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();

        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden anular ventas con transferencia pendiente" });

        var usuario = await dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == sessionUsuarioId && u.IsActive);
        if (usuario == null)
            return Unauthorized(new { message = "Sesion de operador invalida" });

        if (!IsSupervisorOrAdmin(usuario.Role))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Se requiere autorizacion de supervisor o administrador para cancelar esta transferencia" });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Debes ingresar un motivo" });

        using var dedup = _requestDeduplication.Acquire($"sales/{id}/cancel-pending-transfer");
        var sale = await _saleService.CancelPendingTransferAsync(id, sessionUsuarioId, request.Reason);

            await _auditService.LogAsync(
                AuditEventType.Other,
                sessionUsuarioId,
                $"Pending transfer sale cancelled {id}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString(),
                null,
                $"SaleId: {id}, Reason: {request.Reason}",
                true
            );

            if (sale.CashSessionId.HasValue)
                await _cashSessionService.RecalculateTotalsAsync(sale.CashSessionId.Value);

        return Ok(ToSaleResponse(sale));
    }

    [HttpPost("{id}/cancel-pending-transfer/authorize")]
    [OperatorSessionAuth]
    public async Task<ActionResult<SaleResponse>> CancelPendingTransferWithAuthorization(int id, [FromBody] CancelPendingTransferWithAuthorizationRequest request)
    {
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden anular ventas con transferencia pendiente" });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Debes ingresar un motivo" });

        if (string.IsNullOrWhiteSpace(request.ApproverUsername)
            || string.IsNullOrWhiteSpace(request.ApproverPassword)
            || string.IsNullOrWhiteSpace(request.ApproverPin))
            return BadRequest(new { message = "Debes ingresar credenciales del supervisor o administrador" });

        var operador = await dbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == sessionUsuarioId && u.IsActive);
        if (operador == null)
            return Unauthorized(new { message = "Sesion de operador invalida" });

        if (!CanOperatePendingTransfers(operador.Role))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permisos para cancelar transferencias" });

        var approver = await _operatorSessionService.ValidateCredentialsAsync(
            request.ApproverUsername.Trim(),
            request.ApproverPassword,
            request.ApproverPin.Trim());

        if (approver == null)
            return Unauthorized(new { message = "Credenciales del supervisor o administrador invalidas" });

        if (!IsSupervisorOrAdmin(approver.Role))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "La autorizacion debe ser de un supervisor o administrador" });

        if (!await IsUserAllowedForDeviceAsync(approver, device, dbContext))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "El usuario autorizador no esta habilitado para operar en esta caja" });

        using var dedup = _requestDeduplication.Acquire($"sales/{id}/cancel-pending-transfer");
        var sale = await _saleService.CancelPendingTransferAsync(id, sessionUsuarioId, request.Reason);

        await _auditService.LogAsync(
            AuditEventType.Other,
            sessionUsuarioId,
            $"Pending transfer sale cancelled {id} with supervisor authorization",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers.UserAgent.ToString(),
            null,
            $"SaleId: {id}, Reason: {request.Reason}, ApprovedBy: {approver.Username} (#{approver.Id}), Operator: {operador.Username}",
            true
        );

        if (sale.CashSessionId.HasValue)
            await _cashSessionService.RecalculateTotalsAsync(sale.CashSessionId.Value);

        return Ok(ToSaleResponse(sale));
    }

    [HttpPost("{saleId}/returns")]
    [OperatorSessionAuth]
    public async Task<ActionResult<SaleReturnResponse>> CreateReturn(int saleId, [FromBody] CreateSaleReturnRequest request)
    {
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden procesar devoluciones" });

        var cashSession = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
        if (cashSession == null)
            return BadRequest(new { message = "No hay una caja abierta" });

        using var dedup = _requestDeduplication.Acquire($"sales/{saleId}/returns");
        var lines = request.Lines.Select(l => (l.OriginalSaleItemId, l.QtyReturned, l.Condition)).ToList();
        var saleReturn = await _saleService.CreateReturnAsync(
            saleId,
            cashSession.Id,
            operatorSessionId,
            usuarioId,
            request.RefundPreference,
            request.CustomerId,
            request.CustomerAlias,
            lines);

        await _cashSessionService.RecalculateTotalsAsync(cashSession.Id);
        return Ok(ToSaleReturnResponse(saleReturn));
    }

    [HttpGet("returns")]
    [OperatorSessionAuth]
    public async Task<ActionResult<List<SaleReturnResponse>>> GetReturns([FromQuery] int? saleId = null)
    {
        var returns = await _saleService.GetReturnsAsync(saleId);
        return Ok(returns.Select(ToSaleReturnResponse).ToList());
    }

    [HttpGet("returns/{returnId}")]
    [OperatorSessionAuth]
    public async Task<ActionResult<SaleReturnResponse>> GetReturn(int returnId)
    {
        var saleReturn = await _saleService.GetReturnByIdAsync(returnId);
        if (saleReturn == null)
            return NotFound();

        return Ok(ToSaleReturnResponse(saleReturn));
    }

    private static bool CanOperatePendingTransfers(string? role)
    {
        return string.Equals(role, UserRole.Operator.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, UserRole.Supervisor.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupervisorOrAdmin(string? role)
    {
        return string.Equals(role, UserRole.Supervisor.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> IsUserAllowedForDeviceAsync(Usuario usuario, Device? device, Data.ApplicationDbContext dbContext)
    {
        if (device == null)
            return false;

        if (!device.StoreId.HasValue)
            return true;

        var storeId = device.StoreId.Value;
        var storeTenantId = await dbContext.Stores
            .Where(s => s.Id == storeId)
            .Select(s => (int?)s.TenantId)
            .FirstOrDefaultAsync();

        if (usuario.TenantId.HasValue && storeTenantId.HasValue && usuario.TenantId.Value != storeTenantId.Value)
            return false;

        if (!string.Equals(usuario.Role, UserRole.Operator.ToString(), StringComparison.OrdinalIgnoreCase))
            return true;

        return await dbContext.StoreUsers.AnyAsync(su =>
            su.UsuarioId == usuario.Id
            && su.StoreId == storeId
            && su.IsActive);
    }

    private static SaleResponse ToSaleResponse(Sale sale)
    {
        return new SaleResponse
        {
            Id = sale.Id,
            CartId = sale.CartId,
            CustomerId = sale.CustomerId,
            DeviceId = sale.DeviceId,
            Status = sale.Status,
            Subtotal = sale.Subtotal,
            Discount = sale.Discount,
            Tax = sale.Tax,
            Total = sale.Total,
            InvoiceNumber = sale.InvoiceNumber,
            CreatedAt = sale.CreatedAt,
            CompletedAt = sale.CompletedAt,
            ShiftBucket = sale.ShiftBucket,
            ExpectedShiftBucket = sale.ExpectedShiftBucket,
            ShiftAssignmentStatus = sale.ShiftAssignmentStatus,
            ShiftAssignedAt = sale.ShiftAssignedAt,
            LateShiftOpen = sale.LateShiftOpen,
            Items = sale.Items.Select(ToSaleItemResponse).ToList(),
            Payments = sale.Payments.Select(ToSalePaymentResponse).ToList()
        };
    }

    private static SaleItemResponse ToSaleItemResponse(SaleItem item)
    {
        return new SaleItemResponse
        {
            Id = item.Id,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Discount = item.Discount,
            Subtotal = item.Subtotal
        };
    }

    private static SalePaymentResponse ToSalePaymentResponse(SalePayment payment)
    {
        return new SalePaymentResponse
        {
            Id = payment.Id,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            Amount = payment.Amount,
            Reference = payment.Reference,
            Provider = payment.Provider,
            ExternalReference = payment.ExternalReference,
            ConfirmedAt = payment.ConfirmedAt,
            ConfirmNotes = payment.ConfirmNotes,
            CreatedAt = payment.CreatedAt
        };
    }

    private static SaleReturnResponse ToSaleReturnResponse(SaleReturn saleReturn)
    {
        return new SaleReturnResponse
        {
            Id = saleReturn.Id,
            OriginalSaleId = saleReturn.OriginalSaleId,
            CashSessionId = saleReturn.CashSessionId,
            RefundPreference = saleReturn.RefundPreference,
            RefundTotal = saleReturn.RefundTotal,
            ReturnedSubtotal = saleReturn.ReturnedSubtotal,
            ReturnedCigaretteSurchargeShare = saleReturn.ReturnedCigaretteSurchargeShare,
            CustomerId = saleReturn.CustomerId,
            CustomerAlias = saleReturn.CustomerAlias,
            CreatedAt = saleReturn.CreatedAt,
            Lines = saleReturn.Lines.Select(l => new SaleReturnLineResponse
            {
                Id = l.Id,
                OriginalSaleItemId = l.OriginalSaleItemId,
                ProductId = l.ProductId,
                QtyReturned = l.QtyReturned,
                Condition = l.Condition,
                LineRefundAmount = l.LineRefundAmount,
                IsCigarette = l.IsCigarette
            }).ToList()
        };
    }

}

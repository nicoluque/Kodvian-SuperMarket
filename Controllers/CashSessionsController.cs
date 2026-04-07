using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;
using Microsoft.EntityFrameworkCore;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/cash-sessions")]
public class CashSessionsController : ControllerBase
{
    private readonly ICashSessionService _cashSessionService;
    private readonly IOperatorSessionService _operatorSessionService;
    private readonly IShiftCloseGate _shiftCloseGate;
    private readonly ICigaretteCountService _cigaretteCountService;
    private readonly IRequestDeduplicationService _requestDeduplication;
    private readonly IStoreShiftService _storeShiftService;

    public CashSessionsController(ICashSessionService cashSessionService, IOperatorSessionService operatorSessionService, IShiftCloseGate shiftCloseGate, ICigaretteCountService cigaretteCountService, IRequestDeduplicationService requestDeduplication, IStoreShiftService storeShiftService)
    {
        _cashSessionService = cashSessionService;
        _operatorSessionService = operatorSessionService;
        _shiftCloseGate = shiftCloseGate;
        _cigaretteCountService = cigaretteCountService;
        _requestDeduplication = requestDeduplication;
        _storeShiftService = storeShiftService;
    }

    [HttpPost("open")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionResponse>> Open([FromBody] CashSessionOpenRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var sessionId = (int)HttpContext.Items["SessionId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden abrir una sesión de caja" });

        try
        {
            var session = await _cashSessionService.OpenAsync(deviceId, sessionId, request.Shift, request.OpeningCash);

            var sessionUsuarioId = HttpContext.Items.TryGetValue("SessionUsuarioId", out var uidObj) && uidObj is int uid ? uid : (int?)null;
            if (session.StoreId.HasValue)
            {
                await _storeShiftService.AssignTransitionsOnShiftOpenAsync(session.StoreId.Value, session.Shift, session.OpenedAt, sessionUsuarioId);
            }

            return Ok(await ToResponseAsync(session));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("current")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionResponse>> GetCurrent()
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        CashSession? session = null;

        if (device?.DeviceType == "CashRegister")
        {
            session = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
        }
        else if (device?.DeviceType == "Tablet")
        {
            session = await _cashSessionService.GetCurrentForTabletAsync(deviceId);
        }

        if (session == null)
            return NotFound(new { message = "No hay una caja abierta" });

        return Ok(await ToResponseAsync(session));
    }

    [HttpGet("current/sales")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionSalesListResponse>> GetCurrentSales([FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        CashSession? session = null;
        if (device?.DeviceType == "CashRegister")
        {
            session = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
        }
        else if (device?.DeviceType == "Tablet")
        {
            session = await _cashSessionService.GetCurrentForTabletAsync(deviceId);
        }

        if (session == null)
            return NotFound(new { message = "No hay una caja abierta" });

        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        var baseQuery = dbContext.Sales
            .AsNoTracking()
            .Where(s => s.CashSessionId == session.Id)
            .Include(s => s.Customer)
            .Include(s => s.Payments);

        var totalCount = await baseQuery.CountAsync();
        var totalAmount = await baseQuery.SumAsync(s => (decimal?)s.Total) ?? 0m;

        var rows = await baseQuery
            .OrderByDescending(s => s.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        var items = rows.Select(s => new CashSessionSaleSummaryResponse
        {
            SaleId = s.Id,
            CreatedAt = s.CreatedAt,
            Status = s.Status,
            Total = s.Total,
            CustomerName = string.IsNullOrWhiteSpace(s.Customer?.FullName) ? "Consumidor final" : s.Customer!.FullName,
            PaymentMethodsLabel = string.Join(" + ", s.Payments
                .Select(p => p.PaymentMethod)
                .Distinct()
                .Select(ToPaymentMethodLabel)),
            InvoiceNumber = s.InvoiceNumber
        }).ToList();

        return Ok(new CashSessionSalesListResponse
        {
            CashSessionId = session.Id,
            TotalCount = totalCount,
            TotalAmount = totalAmount,
            Items = items
        });
    }

    [HttpGet("{id}")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionResponse>> Get(int id)
    {
        var session = await _cashSessionService.GetByIdAsync(id);

        if (session == null)
            return NotFound();

        return Ok(await ToResponseAsync(session));
    }

    [HttpPost("{id}/close")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionCloseResponse>> Close(int id, [FromBody] CashSessionCloseRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var sessionId = (int)HttpContext.Items["SessionId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden cerrar una sesión de caja" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();

        if (session.DeviceId != deviceId)
            return Forbid();

        var missingTasks = await _shiftCloseGate.GetMissingRequiredTasksAsync(id);
        missingTasks = missingTasks
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hasCigaretteCount = await _cigaretteCountService.HasCountAsync(id);
        var blockedByCigarettesCount = !hasCigaretteCount;

        var nonBlockingPendingTasks = missingTasks
            .Where(t => !string.Equals(t, "CigaretteCount", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (blockedByCigarettesCount)
        {
            return BadRequest(new
            {
                message = "No se puede cerrar la caja: hay tareas obligatorias pendientes",
                
                blockedByTasks = new[] { "CigaretteCount" },
                missingRequiredTasks = new[] { "CigaretteCount" },
                pendingNonBlockingTasks = nonBlockingPendingTasks,
                blockedByCigarettesCount
            });
        }

        using var dedup = _requestDeduplication.Acquire($"cash-sessions/{id}/close");
        var closedSession = await _cashSessionService.CloseAsync(
            id,
            sessionId,
            request.DeclaredCash,
            request.DeclaredCard,
            request.DeclaredTransfer,
            request.DeclaredCredit,
            request.Notes
        );

        return Ok(new CashSessionCloseResponse
        {
            Session = await ToResponseAsync(closedSession),
            BlockedByTasks = new List<string>(),
            MissingRequiredTasks = new List<string>(),
            BlockedByCigarettesCount = blockedByCigarettesCount,
            HasNonBlockingPendingTasks = nonBlockingPendingTasks.Any(),
            PendingNonBlockingTasks = nonBlockingPendingTasks
        });
    }

    [HttpPost("{id}/handover")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionHandoverResponse>> Handover(int id, [FromBody] CashSessionHandoverRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden transferir mando" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();
        if (session.DeviceId != deviceId)
            return Forbid();

        try
        {
            var handover = await _cashSessionService.HandoverAsync(id, operatorSessionId, request.Reason, request.Notes);
            return Ok(await ToHandoverResponseAsync(handover));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/handover-auth")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionHandoverAuthResponse>> HandoverWithAuth(int id, [FromBody] CashSessionHandoverAuthRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var fromOperatorSessionId = (int)HttpContext.Items["SessionId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden transferir mando" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();
        if (session.DeviceId != deviceId)
            return Forbid();

        var username = request.NewOperatorUsername?.Trim() ?? string.Empty;
        var password = request.NewOperatorPassword ?? string.Empty;
        var pin = request.NewOperatorPin?.Trim() ?? string.Empty;

        var usuario = await _operatorSessionService.ValidateCredentialsAsync(username, password, pin);
        if (usuario == null)
            return Unauthorized(new { message = "Credenciales del nuevo operador inválidas" });

        if (session.CurrentUsuarioId.HasValue && session.CurrentUsuarioId.Value == usuario.Id)
            return BadRequest(new { message = "El operador ingresado ya tiene el mando de esta caja" });

        if (!await IsUserAllowedForDeviceAsync(usuario, device, dbContext))
            return BadRequest(new { message = "El operador ingresado no está habilitado para operar en esta caja" });

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var deviceType = Request.Headers["Device-Type"].FirstOrDefault();

            var (newOperatorSession, rawToken) = await _operatorSessionService.CreateAsync(
                usuario.Id,
                ipAddress,
                userAgent,
                deviceType,
                $"Cash handover from operator-session {fromOperatorSessionId}"
            );

            var handover = await _cashSessionService.HandoverAsync(id, newOperatorSession.Id, request.Reason, request.Notes);

            return Ok(new CashSessionHandoverAuthResponse
            {
                Handover = await ToHandoverResponseAsync(handover),
                OperatorSession = new OperatorSessionResponse
                {
                    SessionToken = rawToken,
                    ExpiresAt = newOperatorSession.ExpiresAt,
                    UsuarioId = usuario.Id,
                    Username = usuario.Username,
                    Role = usuario.Role
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/handover-history")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<List<CashSessionHandoverResponse>>> GetHandoverHistory(int id)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();
        if (session.DeviceId != deviceId)
            return Forbid();

        var history = await _cashSessionService.GetHandoverHistoryAsync(id);
        var mapped = new List<CashSessionHandoverResponse>();
        foreach (var item in history)
        {
            mapped.Add(await ToHandoverResponseAsync(item));
        }

        return Ok(mapped);
    }

    [HttpPost("{id}/cigarettes-count")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CigaretteCountDto>> CreateCigaretteCount(int id, [FromBody] CreateCigaretteCountDto request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden registrar conteos de cigarrillos" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();

        if (session.DeviceId != deviceId)
            return Forbid();

        if (session.Status != CashSessionStatus.Open.ToString())
            return BadRequest(new { message = "Can only count cigarettes for open sessions" });

        try
        {
            var lines = request.Lines.Select(l => (l.ProductId, l.CountedQty)).ToList();
            var count = await _cigaretteCountService.CreateCountAsync(id, request.Notes, lines);
            return Ok(ToCigaretteCountDto(count));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/cigarettes-count/apply-adjustments")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CigaretteCountDto>> ApplyCigaretteAdjustments(int id)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);

        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden aplicar ajustes de cigarrillos" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();

        if (session.DeviceId != deviceId)
            return Forbid();

        try
        {
            var count = await _cigaretteCountService.ApplyAdjustmentsAsync(id);
            return Ok(ToCigaretteCountDto(count));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/money-movements")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionMoneyMovementResponse>> CreateMoneyMovement(int id, [FromBody] CreateCashSessionMoneyMovementRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden registrar movimientos de dinero" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();
        if (session.DeviceId != deviceId)
            return Forbid();
        if (session.Status != CashSessionStatus.Open.ToString())
            return BadRequest(new { message = "CashSession must be open" });

        var user = await dbContext.Usuarios.FindAsync(usuarioId);
        if (user == null)
            return Unauthorized();

        var managerOnlyTypes = new[] { CashSessionMovementType.Withdrawal.ToString(), CashSessionMovementType.Correction.ToString() };
        if (managerOnlyTypes.Contains(request.Type) && user.Role != UserRole.Admin.ToString() && user.Role != UserRole.Supervisor.ToString())
            return Forbid();

        var cashierAllowed = new[] { CashSessionMovementType.Expense.ToString(), CashSessionMovementType.Deposit.ToString(), CashSessionMovementType.Refund.ToString() };
        if (!cashierAllowed.Contains(request.Type) && !managerOnlyTypes.Contains(request.Type))
            return BadRequest(new { message = "Tipo de movimiento inválido" });

        try
        {
            var movement = await _cashSessionService.CreateMoneyMovementAsync(
                id,
                operatorSessionId,
                usuarioId,
                request.Method,
                request.Amount,
                request.Type,
                request.Reason,
                request.Category,
                request.RefType,
                request.RefId
            );

            return Ok(ToMoneyMovementResponse(movement));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/money-movements")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<List<CashSessionMoneyMovementResponse>>> GetMoneyMovements(int id, [FromQuery] string? method = null, [FromQuery] string? type = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden listar movimientos de dinero" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();
        if (session.DeviceId != deviceId)
            return Forbid();

        var movements = await _cashSessionService.GetMoneyMovementsAsync(id, method, type, from, to);
        return Ok(movements.Select(ToMoneyMovementResponse).ToList());
    }

    [HttpGet("{id}/money-movements/summary")]
    [DeviceAuth]
    [OperatorSessionAuth]
    public async Task<ActionResult<CashSessionMoneyMovementSummaryResponse>> GetMoneyMovementsSummary(int id)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await dbContext.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden ver el resumen de movimientos" });

        var session = await _cashSessionService.GetByIdAsync(id);
        if (session == null)
            return NotFound();
        if (session.DeviceId != deviceId)
            return Forbid();

        var movements = await _cashSessionService.GetMoneyMovementsAsync(id);
        var summary = new CashSessionMoneyMovementSummaryResponse
        {
            CashSessionId = id,
            TotalSignedAmount = movements.Sum(m => m.SignedAmount),
            Cash = movements.Where(m => m.Method == PaymentMethod.Cash.ToString()).Sum(m => m.SignedAmount),
            Card = movements.Where(m => m.Method == PaymentMethod.Card.ToString()).Sum(m => m.SignedAmount),
            Transfer = movements.Where(m => m.Method == PaymentMethod.Transfer.ToString()).Sum(m => m.SignedAmount),
            Credit = movements.Where(m => m.Method == PaymentMethod.Credit.ToString() || m.Method == PaymentMethod.AccountCredit.ToString()).Sum(m => m.SignedAmount)
        };
        return Ok(summary);
    }

    private async Task<CashSessionResponse> ToResponseAsync(CashSession session)
    {
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();

        var userIds = new[]
        {
            session.OpenedByUsuarioId,
            session.CurrentUsuarioId,
            session.ClosedByUsuarioId
        }
        .Where(x => x.HasValue)
        .Select(x => x!.Value)
        .Distinct()
        .ToList();

        var usernameById = userIds.Count == 0
            ? new Dictionary<int, string>()
            : await dbContext.Usuarios
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username);

        return new CashSessionResponse
        {
            Id = session.Id,
            DeviceId = session.DeviceId,
            DeviceName = session.Device?.DeviceName ?? string.Empty,
            Shift = session.Shift,
            Status = session.Status,
            OpeningCash = session.OpeningCash,
            TotalCash = session.TotalCash,
            TotalCard = session.TotalCard,
            TotalTransfer = session.TotalTransfer,
            TotalCredit = session.TotalCredit,
            TotalSales = session.TotalSales,
            DeclaredCash = session.DeclaredCash,
            DeclaredCard = session.DeclaredCard,
            DeclaredTransfer = session.DeclaredTransfer,
            DeclaredCredit = session.DeclaredCredit,
            DeclaredTotal = session.DeclaredTotal,
            DiffCash = session.DiffCash,
            DiffCard = session.DiffCard,
            DiffTransfer = session.DiffTransfer,
            DiffCredit = session.DiffCredit,
            DiffTotal = session.DiffTotal,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            CloseNotes = session.CloseNotes,
            SalesCount = session.Sales?.Count ?? 0,
            OpenedByUsuarioId = session.OpenedByUsuarioId,
            OpenedByUsername = session.OpenedByUsuarioId.HasValue && usernameById.ContainsKey(session.OpenedByUsuarioId.Value) ? usernameById[session.OpenedByUsuarioId.Value] : null,
            CurrentUsuarioId = session.CurrentUsuarioId,
            CurrentUsername = session.CurrentUsuarioId.HasValue && usernameById.ContainsKey(session.CurrentUsuarioId.Value) ? usernameById[session.CurrentUsuarioId.Value] : null,
            ClosedByUsuarioId = session.ClosedByUsuarioId,
            ClosedByUsername = session.ClosedByUsuarioId.HasValue && usernameById.ContainsKey(session.ClosedByUsuarioId.Value) ? usernameById[session.ClosedByUsuarioId.Value] : null
        };
    }

    private async Task<CashSessionHandoverResponse> ToHandoverResponseAsync(CashSessionHandover handover)
    {
        var dbContext = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var userIds = new[] { handover.FromUsuarioId, handover.ToUsuarioId }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var usernameById = userIds.Count == 0
            ? new Dictionary<int, string>()
            : await dbContext.Usuarios
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username);

        return new CashSessionHandoverResponse
        {
            Id = handover.Id,
            CashSessionId = handover.CashSessionId,
            FromUsuarioId = handover.FromUsuarioId,
            FromUsername = handover.FromUsuarioId.HasValue && usernameById.ContainsKey(handover.FromUsuarioId.Value)
                ? usernameById[handover.FromUsuarioId.Value]
                : null,
            ToUsuarioId = handover.ToUsuarioId,
            ToUsername = usernameById.ContainsKey(handover.ToUsuarioId) ? usernameById[handover.ToUsuarioId] : $"Usuario #{handover.ToUsuarioId}",
            Reason = handover.Reason,
            Notes = handover.Notes,
            CreatedAt = handover.CreatedAt
        };
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

        var isOperator = usuario.Role == UserRole.Operator.ToString();
        if (!isOperator)
            return true;

        return await dbContext.StoreUsers.AnyAsync(su =>
            su.UsuarioId == usuario.Id &&
            su.StoreId == storeId &&
            su.IsActive);
    }

    private static CigaretteCountDto ToCigaretteCountDto(CigaretteCount count)
    {
        return new CigaretteCountDto
        {
            Id = count.Id,
            CashSessionId = count.CashSessionId,
            CountDate = count.CountDate,
            Notes = count.Notes,
            AdjustmentsApplied = count.AdjustmentsApplied,
            AdjustmentsAppliedAt = count.AdjustmentsAppliedAt,
            Lines = count.Lines.Select(l => new CigaretteCountLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductCode = l.Product?.Barcode,
                ProductName = l.Product?.Name,
                SystemQtyAtCount = l.SystemQtyAtCount,
                CountedQty = l.CountedQty,
                DiffQty = l.DiffQty,
                AdjustmentApplied = l.AdjustmentApplied
            }).ToList()
        };
    }

    private static CashSessionMoneyMovementResponse ToMoneyMovementResponse(CashSessionMoneyMovement m)
    {
        return new CashSessionMoneyMovementResponse
        {
            Id = m.Id,
            CashSessionId = m.CashSessionId,
            Method = m.Method,
            SignedAmount = m.SignedAmount,
            Type = m.Type,
            Reason = m.Reason,
            Category = m.Category,
            RefType = m.RefType,
            RefId = m.RefId,
            CreatedByOperatorSessionId = m.CreatedByOperatorSessionId,
            CreatedByUsuarioId = m.CreatedByUsuarioId,
            CreatedAt = m.CreatedAt
        };
    }

    private static string ToPaymentMethodLabel(string method)
    {
        if (method == PaymentMethod.Cash.ToString()) return "Efectivo";
        if (method == PaymentMethod.Card.ToString()) return "Tarjeta";
        if (method == PaymentMethod.Transfer.ToString()) return "Transferencia";
        if (method == PaymentMethod.QrMp.ToString()) return "QR";
        if (method == PaymentMethod.AccountCredit.ToString()) return "Cuenta corriente";
        return method;
    }
}

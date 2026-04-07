using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface ICashSessionService
{
    Task<CashSession> OpenAsync(int deviceId, int operatorSessionId, string shift, decimal openingCash);
    Task<CashSession?> GetCurrentForDeviceAsync(int deviceId);
    Task<CashSession?> GetCurrentForTabletAsync(int tabletDeviceId);
    Task<CashSession?> GetByIdAsync(int id);
    Task<CashSession> CloseAsync(int sessionId, int operatorSessionId, decimal declaredCash, decimal declaredCard, decimal declaredTransfer, decimal declaredCredit, string? notes);
    Task<CashSessionHandover> HandoverAsync(int sessionId, int operatorSessionId, string reason, string? notes);
    Task<List<CashSessionHandover>> GetHandoverHistoryAsync(int sessionId);
    Task RecalculateTotalsAsync(int sessionId);
    Task<CashSessionMoneyMovement> CreateMoneyMovementAsync(int cashSessionId, int operatorSessionId, int usuarioId, string method, decimal amount, string type, string reason, string? category, string? refType, int? refId);
    Task<List<CashSessionMoneyMovement>> GetMoneyMovementsAsync(int cashSessionId, string? method = null, string? type = null, DateTime? from = null, DateTime? to = null);
}

public class CashSessionService : ICashSessionService
{
    private readonly ApplicationDbContext _context;
    private readonly IKanbanService _kanbanService;
    private readonly ISaleService _saleService;

    public CashSessionService(ApplicationDbContext context, IKanbanService kanbanService, ISaleService saleService)
    {
        _context = context;
        _kanbanService = kanbanService;
        _saleService = saleService;
    }

    public async Task<CashSession> OpenAsync(int deviceId, int operatorSessionId, string shift, decimal openingCash)
    {
        var existingOpen = await _context.CashSessions
            .FirstOrDefaultAsync(cs => cs.DeviceId == deviceId && cs.Status == CashSessionStatus.Open.ToString());

        if (existingOpen != null)
            throw new InvalidOperationException("Ya existe una sesión de caja abierta para este dispositivo");

        var storeId = await _context.Devices
            .Where(d => d.Id == deviceId)
            .Select(d => d.StoreId)
            .FirstOrDefaultAsync();

        var openerUsuarioId = await _context.OperatorSessions
            .Where(os => os.Id == operatorSessionId)
            .Select(os => (int?)os.UsuarioId)
            .FirstOrDefaultAsync();

        var session = new CashSession
        {
            DeviceId = deviceId,
            StoreId = storeId,
            OperatorSessionId = operatorSessionId,
            OpenedByOperatorSessionId = operatorSessionId,
            OpenedByUsuarioId = openerUsuarioId,
            CurrentOperatorSessionId = operatorSessionId,
            CurrentUsuarioId = openerUsuarioId,
            Shift = shift,
            Status = CashSessionStatus.Open.ToString(),
            OpeningCash = openingCash,
            OpenedAt = DateTime.UtcNow
        };

        _context.CashSessions.Add(session);
        await _context.SaveChangesAsync();

        await _kanbanService.GenerateForCashSessionAsync(session.Id);

        var assignedQueuedSales = await _saleService.AssignQueuedSalesToCashSessionAsync(session.Id, deviceId, operatorSessionId);
        if (assignedQueuedSales > 0)
            await RecalculateTotalsAsync(session.Id);

        return session;
    }

    public async Task<CashSession?> GetCurrentForDeviceAsync(int deviceId)
    {
        return await _context.CashSessions
            .Include(cs => cs.Sales)
            .FirstOrDefaultAsync(cs => cs.DeviceId == deviceId && cs.Status == CashSessionStatus.Open.ToString());
    }

    public async Task<CashSession?> GetCurrentForTabletAsync(int tabletDeviceId)
    {
        var tablet = await _context.Devices.FindAsync(tabletDeviceId);
        if (tablet?.ParentCashRegisterDeviceId == null)
            return null;

        return await GetCurrentForDeviceAsync(tablet.ParentCashRegisterDeviceId.Value);
    }

    public async Task<CashSession?> GetByIdAsync(int id)
    {
        return await _context.CashSessions
            .Include(cs => cs.Sales)
            .FirstOrDefaultAsync(cs => cs.Id == id);
    }

    public async Task<CashSession> CloseAsync(int sessionId, int operatorSessionId, decimal declaredCash, decimal declaredCard, decimal declaredTransfer, decimal declaredCredit, string? notes)
    {
        var session = await _context.CashSessions
            .Include(cs => cs.Sales)
            .FirstOrDefaultAsync(cs => cs.Id == sessionId);

        if (session == null)
            throw new InvalidOperationException("No se encontró la sesión de caja");

        if (session.Status != CashSessionStatus.Open.ToString())
            throw new InvalidOperationException("La sesión de caja no está abierta");

        await RecalculateTotalsAsync(sessionId);

        session = await _context.CashSessions.FindAsync(sessionId);
        
        var closingUsuarioId = await _context.OperatorSessions
            .Where(os => os.Id == operatorSessionId)
            .Select(os => (int?)os.UsuarioId)
            .FirstOrDefaultAsync();

        session!.DeclaredCash = declaredCash;
        session.DeclaredCard = declaredCard;
        session.DeclaredTransfer = declaredTransfer;
        session.DeclaredCredit = declaredCredit;
        session.CloseNotes = notes;
        session.OperatorSessionId = operatorSessionId;
        session.CurrentOperatorSessionId = operatorSessionId;
        session.CurrentUsuarioId = closingUsuarioId;
        session.ClosedByOperatorSessionId = operatorSessionId;
        session.ClosedByUsuarioId = closingUsuarioId;
        session.ClosedAt = DateTime.UtcNow;
        session.Status = CashSessionStatus.Closed.ToString();

        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<CashSessionHandover> HandoverAsync(int sessionId, int operatorSessionId, string reason, string? notes)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Debes ingresar un motivo para el cambio de operador");

        var session = await _context.CashSessions.FirstOrDefaultAsync(cs => cs.Id == sessionId);
        if (session == null)
            throw new InvalidOperationException("No se encontró la sesión de caja");
        if (session.Status != CashSessionStatus.Open.ToString())
            throw new InvalidOperationException("La sesión de caja no está abierta");

        var toUsuarioId = await _context.OperatorSessions
            .Where(os => os.Id == operatorSessionId)
            .Select(os => (int?)os.UsuarioId)
            .FirstOrDefaultAsync();

        if (!toUsuarioId.HasValue)
            throw new InvalidOperationException("Sesión de operador inválida");

        var handover = new CashSessionHandover
        {
            CashSessionId = sessionId,
            FromOperatorSessionId = session.CurrentOperatorSessionId,
            FromUsuarioId = session.CurrentUsuarioId,
            ToOperatorSessionId = operatorSessionId,
            ToUsuarioId = toUsuarioId.Value,
            Reason = reason.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.CashSessionHandovers.Add(handover);

        session.OperatorSessionId = operatorSessionId;
        session.CurrentOperatorSessionId = operatorSessionId;
        session.CurrentUsuarioId = toUsuarioId.Value;

        await _context.SaveChangesAsync();
        return handover;
    }

    public async Task<List<CashSessionHandover>> GetHandoverHistoryAsync(int sessionId)
    {
        return await _context.CashSessionHandovers
            .Where(h => h.CashSessionId == sessionId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task RecalculateTotalsAsync(int sessionId)
    {
        var session = await _context.CashSessions
            .Include(cs => cs.Sales)
                .ThenInclude(s => s.Payments)
            .Include(cs => cs.MoneyMovements)
            .FirstOrDefaultAsync(cs => cs.Id == sessionId);

        if (session == null)
            return;

        var payments = session.Sales
            .SelectMany(s => s.Payments)
            .Where(p => p.Status == PaymentStatus.Confirmed.ToString())
            .ToList();

        var moneyMovements = session.MoneyMovements.ToList();

        session.TotalCash = payments.Where(p => p.PaymentMethod == PaymentMethod.Cash.ToString()).Sum(p => p.Amount)
            + moneyMovements.Where(m => m.Method == PaymentMethod.Cash.ToString()).Sum(m => m.SignedAmount);
        session.TotalCard = payments.Where(p => p.PaymentMethod == PaymentMethod.Card.ToString()).Sum(p => p.Amount)
            + moneyMovements.Where(m => m.Method == PaymentMethod.Card.ToString()).Sum(m => m.SignedAmount);
        session.TotalTransfer = payments.Where(p => p.PaymentMethod == PaymentMethod.Transfer.ToString()).Sum(p => p.Amount)
            + moneyMovements.Where(m => m.Method == PaymentMethod.Transfer.ToString()).Sum(m => m.SignedAmount);
        session.TotalCredit = payments.Where(p => p.PaymentMethod == PaymentMethod.Credit.ToString()).Sum(p => p.Amount)
            + payments.Where(p => p.PaymentMethod == PaymentMethod.AccountCredit.ToString()).Sum(p => p.Amount)
            + moneyMovements.Where(m => m.Method == PaymentMethod.Credit.ToString() || m.Method == PaymentMethod.AccountCredit.ToString()).Sum(m => m.SignedAmount);

        await _context.SaveChangesAsync();
    }

    public async Task<CashSessionMoneyMovement> CreateMoneyMovementAsync(int cashSessionId, int operatorSessionId, int usuarioId, string method, decimal amount, string type, string reason, string? category, string? refType, int? refId)
    {
        var session = await _context.CashSessions.FirstOrDefaultAsync(cs => cs.Id == cashSessionId);
        if (session == null)
            throw new InvalidOperationException("No se encontró la sesión de caja");
        if (session.Status != CashSessionStatus.Open.ToString())
            throw new InvalidOperationException("La sesión de caja debe estar abierta");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Debes ingresar un motivo");

        var normalizedType = type.Trim();
        var normalizedMethod = method.Trim();
        var normalizedAmount = Math.Abs(amount);

        if (normalizedAmount <= 0)
            throw new InvalidOperationException("El monto debe ser mayor a cero");

        var allowedTypes = new[]
        {
            CashSessionMovementType.Refund.ToString(),
            CashSessionMovementType.Expense.ToString(),
            CashSessionMovementType.Withdrawal.ToString(),
            CashSessionMovementType.Deposit.ToString(),
            CashSessionMovementType.Correction.ToString()
        };
        if (!allowedTypes.Contains(normalizedType))
            throw new InvalidOperationException("Tipo de movimiento inválido");

        var allowedMethods = new[]
        {
            PaymentMethod.Cash.ToString(),
            PaymentMethod.Card.ToString(),
            PaymentMethod.Transfer.ToString(),
            PaymentMethod.Credit.ToString(),
            PaymentMethod.AccountCredit.ToString()
        };
        if (!allowedMethods.Contains(normalizedMethod))
            throw new InvalidOperationException("Método de movimiento inválido");

        var signedAmount = normalizedType switch
        {
            var t when t == CashSessionMovementType.Expense.ToString() => -normalizedAmount,
            var t when t == CashSessionMovementType.Withdrawal.ToString() => -normalizedAmount,
            var t when t == CashSessionMovementType.Refund.ToString() => -normalizedAmount,
            var t when t == CashSessionMovementType.Deposit.ToString() => normalizedAmount,
            _ => amount
        };

        var movement = new CashSessionMoneyMovement
        {
            CashSessionId = cashSessionId,
            StoreId = session.StoreId,
            Method = normalizedMethod,
            SignedAmount = signedAmount,
            Type = normalizedType,
            Reason = reason,
            Category = category,
            RefType = refType,
            RefId = refId,
            CreatedByOperatorSessionId = operatorSessionId,
            CreatedByUsuarioId = usuarioId,
            CreatedAt = DateTime.UtcNow
        };

        _context.CashSessionMoneyMovements.Add(movement);
        await _context.SaveChangesAsync();

        await RecalculateTotalsAsync(cashSessionId);
        return movement;
    }

    public async Task<List<CashSessionMoneyMovement>> GetMoneyMovementsAsync(int cashSessionId, string? method = null, string? type = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.CashSessionMoneyMovements
            .Where(m => m.CashSessionId == cashSessionId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(method))
            query = query.Where(m => m.Method == method);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(m => m.Type == type);
        if (from.HasValue)
            query = query.Where(m => m.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.CreatedAt <= to.Value);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }
}

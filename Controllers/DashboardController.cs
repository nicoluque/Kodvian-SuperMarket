using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[OperatorSessionAuth]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> Summary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();

        var start = (from?.Date ?? DateTime.UtcNow.Date).ToUniversalTime();
        var end = ((to?.Date ?? DateTime.UtcNow.Date).AddDays(1)).ToUniversalTime();

        var sales = await _db.Sales
            .Where(s => s.CreatedAt >= start && s.CreatedAt < end && s.Status != SaleStatus.Cancelled.ToString() && s.Status != SaleStatus.Voided.ToString())
            .Where(s => !activeStoreId.HasValue || s.StoreId == activeStoreId.Value)
            .ToListAsync();

        var salesTotal = sales.Sum(s => s.Total);
        var tickets = sales.Count;
        var avgTicket = tickets > 0 ? salesTotal / tickets : 0m;

        var pendingTransfers = await _db.Sales.CountAsync(s => s.Status == SaleStatus.PendingTransfer.ToString() && (!activeStoreId.HasValue || s.StoreId == activeStoreId.Value));

        var accountMoves = await _db.CustomerAccountMovements.ToListAsync();
        var accountBalance = accountMoves.Sum(m => m.Amount);

        var merma = await _db.StockMovements
            .Where(m => m.Bucket == StockBucket.MERMA.ToString() && m.CreatedAt >= start && m.CreatedAt < end)
            .Where(m => !activeStoreId.HasValue || m.StoreId == activeStoreId.Value)
            .SumAsync(m => (decimal?)Math.Abs(m.DeltaQty)) ?? 0m;

        var supplierCredits = await _db.SupplierCredits.SumAsync(c => (decimal?)c.RemainingAmount) ?? 0m;

        return Ok(new
        {
            from = start,
            to = end.AddSeconds(-1),
            sales = salesTotal,
            tickets,
            avgTicket,
            pendingTransfers,
            accountBalance,
            waste = merma,
            supplierCredits
        });
    }

    [HttpGet("sales-series")]
    public async Task<ActionResult<List<object>>> SalesSeries([FromQuery] int days = 7)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        if (days <= 0) days = 7;
        if (days > 90) days = 90;

        var end = DateTime.UtcNow.Date.AddDays(1).ToUniversalTime();
        var start = end.AddDays(-days).ToUniversalTime();

        var sales = await _db.Sales
            .Where(s => s.CreatedAt >= start && s.CreatedAt < end && s.Status != SaleStatus.Cancelled.ToString() && s.Status != SaleStatus.Voided.ToString())
            .Where(s => !activeStoreId.HasValue || s.StoreId == activeStoreId.Value)
            .ToListAsync();

        var result = Enumerable.Range(0, days)
            .Select(i => start.AddDays(i))
            .Select(d =>
            {
                var next = d.AddDays(1);
                var daySales = sales.Where(s => s.CreatedAt >= d && s.CreatedAt < next).ToList();
                return new
                {
                    date = d.ToString("yyyy-MM-dd"),
                    total = daySales.Sum(s => s.Total),
                    tickets = daySales.Count,
                    avgTicket = daySales.Count > 0 ? daySales.Sum(s => s.Total) / daySales.Count : 0m
                };
            })
            .Cast<object>()
            .ToList();

        return Ok(result);
    }

    [HttpGet("payment-methods")]
    public async Task<ActionResult<object>> PaymentMethods([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        var start = (from?.Date ?? DateTime.UtcNow.Date).ToUniversalTime();
        var end = ((to?.Date ?? DateTime.UtcNow.Date).AddDays(1)).ToUniversalTime();

        var payments = await _db.SalePayments
            .Where(p => p.CreatedAt >= start && p.CreatedAt < end)
            .Where(p => !activeStoreId.HasValue || p.Sale.StoreId == activeStoreId.Value)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new { method = g.Key, amount = g.Sum(x => x.Amount), count = g.Count() })
            .OrderByDescending(x => x.amount)
            .ToListAsync();

        return Ok(new { from = start, to = end.AddSeconds(-1), items = payments });
    }

    [HttpGet("top-credit-debtors")]
    public async Task<ActionResult<List<object>>> TopCreditDebtors([FromQuery] int top = 10)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        if (top <= 0) top = 10;

        var customers = await _db.Customers.ToListAsync();
        var moves = await _db.CustomerAccountMovements.ToListAsync();

        var data = moves
            .GroupBy(m => m.CustomerId)
            .Select(g => new
            {
                customerId = g.Key,
                debt = g.Sum(x => x.Amount)
            })
            .Where(x => x.debt > 0)
            .OrderByDescending(x => x.debt)
            .Take(top)
            .Select(x => new
            {
                x.customerId,
                customerName = customers.FirstOrDefault(c => c.Id == x.customerId)?.FullName,
                debt = x.debt
            })
            .Cast<object>()
            .ToList();

        return Ok(data);
    }

    [HttpGet("top-container-debtors")]
    public async Task<ActionResult<List<object>>> TopContainerDebtors([FromQuery] int top = 10)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        if (top <= 0) top = 10;

        var customers = await _db.Customers.ToListAsync();
        var moves = await _db.ContainerMovements.ToListAsync();

        var data = moves
            .GroupBy(m => m.CustomerId)
            .Select(g => new
            {
                customerId = g.Key,
                qty = g.Sum(x => x.Direction == ContainerDirection.Given.ToString() ? x.Qty : -x.Qty)
            })
            .Where(x => x.qty > 0)
            .OrderByDescending(x => x.qty)
            .Take(top)
            .Select(x => new
            {
                x.customerId,
                customerName = customers.FirstOrDefault(c => c.Id == x.customerId)?.FullName,
                containerDebtQty = x.qty
            })
            .Cast<object>()
            .ToList();

        return Ok(data);
    }

    [HttpGet("critical-stock")]
    public async Task<ActionResult<List<object>>> CriticalStock()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();

        const decimal threshold = 5m;
        var critical = await _db.ProductStocks
            .Include(ps => ps.Product)
            .Where(ps => ps.Bucket == StockBucket.VENDIBLE.ToString() && ps.Quantity <= threshold && ps.Product.StockControl)
            .Where(ps => !activeStoreId.HasValue || ps.StoreId == activeStoreId.Value)
            .OrderBy(ps => ps.Quantity)
            .Take(50)
            .Select(ps => new
            {
                productId = ps.ProductId,
                productCode = ps.Product.Barcode,
                productName = ps.Product.Name,
                vendibleQty = ps.Quantity,
                threshold
            })
            .ToListAsync();

        return Ok(critical);
    }

    [HttpGet("waste-summary")]
    public async Task<ActionResult<object>> WasteSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();
        var start = (from?.Date ?? DateTime.UtcNow.Date).ToUniversalTime();
        var end = ((to?.Date ?? DateTime.UtcNow.Date).AddDays(1)).ToUniversalTime();

        var wasteMoves = await _db.StockMovements
            .Include(m => m.Product)
            .Where(m => m.Bucket == StockBucket.MERMA.ToString() && m.CreatedAt >= start && m.CreatedAt < end)
            .Where(m => !activeStoreId.HasValue || m.StoreId == activeStoreId.Value)
            .ToListAsync();

        var totalQty = wasteMoves.Sum(m => Math.Abs(m.DeltaQty));
        var estimatedCost = wasteMoves.Sum(m => Math.Abs(m.DeltaQty) * (m.Product?.LastCost > 0 ? m.Product.LastCost : m.Product?.DefaultPrice ?? 0m));

        var byProduct = wasteMoves
            .GroupBy(m => new { m.ProductId, m.Product!.Name })
            .Select(g => new
            {
                productId = g.Key.ProductId,
                productName = g.Key.Name,
                qty = g.Sum(x => Math.Abs(x.DeltaQty))
            })
            .OrderByDescending(x => x.qty)
            .Take(20)
            .ToList();

        return Ok(new { from = start, to = end.AddSeconds(-1), totalQty, estimatedCost, byProduct });
    }

    [HttpGet("operations-summary")]
    public async Task<ActionResult<object>> OperationsSummary()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var activeStoreId = GetActiveStoreId();

        var pendingTransfers = await _db.Sales
            .Where(s => s.Status == SaleStatus.PendingTransfer.ToString())
            .Where(s => !activeStoreId.HasValue || s.StoreId == activeStoreId.Value)
            .OrderByDescending(s => s.CreatedAt)
            .Take(20)
            .Select(s => new { s.Id, s.Total, s.CreatedAt, s.DeviceId })
            .ToListAsync();

        var pendingClaims = await _db.SupplierClaims
            .Where(c => c.Status == SupplierClaimStatus.Pending.ToString())
            .Where(c => !activeStoreId.HasValue || c.StoreId == activeStoreId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .Take(20)
            .Select(c => new { c.Id, c.SupplierId, c.Status, c.CreatedAt, c.Notes })
            .ToListAsync();

        var openTimePunches = await _db.TimePunches.CountAsync(tp => tp.IsOpen);
        var extrasPending = await _db.EmployeeExtras.CountAsync(e => !e.IsApproved);
        var kanbanPending = await _db.KanbanTasks.CountAsync(k => k.Status != KanbanTaskStatus.Done.ToString() && k.IsRequiredForShiftClose);

        var salesByShift = await _db.CashSessions
            .Where(cs => cs.Status == CashSessionStatus.Open.ToString() || cs.Status == CashSessionStatus.Closed.ToString())
            .Where(cs => !activeStoreId.HasValue || cs.StoreId == activeStoreId.Value)
            .GroupBy(cs => cs.Shift)
            .Select(g => new { shift = g.Key, totalSales = g.Sum(x => x.TotalCash + x.TotalCard + x.TotalTransfer + x.TotalCredit), sessions = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            pendingTransfers,
            pendingClaims,
            rrhhPending = new { openTimePunches, extrasPending },
            kanbanPendingRequired = kanbanPending,
            salesByShift
        });
    }

    private async Task<bool> IsAdminOrManagerAsync()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var userIdObj) || userIdObj is not int userId)
            return false;

        var user = await _db.Usuarios.FindAsync(userId);
        if (user == null)
            return false;

        return user.Role == UserRole.Admin.ToString() || user.Role == UserRole.Supervisor.ToString() || user.Role == "Manager";
    }

    private int? GetActiveStoreId()
    {
        var raw = Request.Headers["X-Store-Id"].FirstOrDefault();
        if (int.TryParse(raw, out var id)) return id;
        return null;
    }
}

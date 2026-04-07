using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/exports")]
public class ExportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmergencyExportService _emergencyExportService;
    private readonly IExportService _exportService;

    public ExportsController(ApplicationDbContext db, IEmergencyExportService emergencyExportService, IExportService exportService)
    {
        _db = db;
        _emergencyExportService = emergencyExportService;
        _exportService = exportService;
    }

    [HttpGet("emergency-catalog")]
    public async Task<IActionResult> EmergencyCatalog([FromQuery] int top = 100, [FromQuery] bool includeNoBarcode = true, [FromQuery] bool includeCigarettes = true)
    {
        var tenantId = HttpContext.Items.TryGetValue("TenantId", out var tenantObj) && tenantObj is int t ? t : (int?)null;
        var bytes = await _emergencyExportService.GenerateEmergencyCatalogPdfAsync(top, includeNoBarcode, includeCigarettes, tenantId);
        return File(bytes, "application/pdf", $"emergency-catalog-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
    }

    [HttpGet("sales/daily")]
    public async Task<IActionResult> SalesDaily([FromQuery] DateTime? date = null, [FromQuery] string format = "xlsx")
    {
        var start = date?.Date ?? DateTime.UtcNow.Date;
        var end = start.AddDays(1);
        var storeId = GetActiveStoreId();

        var sales = await _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.Payments)
            .Where(s => s.CreatedAt >= start && s.CreatedAt < end)
            .Where(s => !storeId.HasValue || s.StoreId == storeId.Value)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var headers = new[] { "SaleId", "StoreId", "CreatedAt", "Status", "Customer", "Subtotal", "Discount", "Tax", "Total", "Payments" };
        var rows = sales.Select(s => (IReadOnlyList<object?>)new object?[]
        {
            s.Id,
            s.StoreId,
            s.CreatedAt,
            s.Status,
            s.Customer?.FullName,
            s.Subtotal,
            s.Discount,
            s.Tax,
            s.Total,
            string.Join(" | ", s.Payments.Select(p => $"{p.PaymentMethod}:{p.Amount:0.00}({p.Status})"))
        }).ToList();

        return await ExportAsync("sales-daily", "Ventas diarias", $"Fecha: {start:yyyy-MM-dd}", headers, rows, format, storeId);
    }

    [HttpGet("sales/range")]
    public async Task<IActionResult> SalesRange([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string format = "xlsx")
    {
        var (start, endExclusive) = GetRange(from, to, 30);
        var storeId = GetActiveStoreId();

        var sales = await _db.Sales
            .Include(s => s.Customer)
            .Where(s => s.CreatedAt >= start && s.CreatedAt < endExclusive)
            .Where(s => !storeId.HasValue || s.StoreId == storeId.Value)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var headers = new[] { "SaleId", "StoreId", "CreatedAt", "Status", "Customer", "Subtotal", "Discount", "Tax", "Total" };
        var rows = sales.Select(s => (IReadOnlyList<object?>)new object?[]
        {
            s.Id, s.StoreId, s.CreatedAt, s.Status, s.Customer?.FullName, s.Subtotal, s.Discount, s.Tax, s.Total
        }).ToList();

        return await ExportAsync("sales-range", "Ventas por rango", $"Desde {start:yyyy-MM-dd} hasta {endExclusive.AddSeconds(-1):yyyy-MM-dd}", headers, rows, format, storeId);
    }

    [HttpGet("customers/account-summary")]
    public async Task<IActionResult> CustomersAccountSummary([FromQuery] string format = "xlsx")
    {
        var customers = await _db.Customers.Where(c => c.IsActive).OrderBy(c => c.FullName).ToListAsync();
        var moves = await _db.CustomerAccountMovements.ToListAsync();

        var grouped = moves.GroupBy(m => m.CustomerId)
            .ToDictionary(g => g.Key, g => new
            {
                Balance = g.Sum(x => x.Amount),
                Count = g.Count(),
                LastDate = g.Max(x => x.CreatedAt)
            });

        var headers = new[] { "CustomerId", "Customer", "AllowsCredit", "CreditLimit", "Balance", "Movements", "LastMovementAt" };
        var rows = customers.Select(c =>
        {
            var info = grouped.TryGetValue(c.Id, out var x) ? x : null;
            return (IReadOnlyList<object?>)new object?[]
            {
                c.Id,
                c.FullName,
                c.AllowsCredit,
                c.CreditLimit,
                info?.Balance ?? 0m,
                info?.Count ?? 0,
                info?.LastDate
            };
        }).ToList();

        return await ExportAsync("customers-account-summary", "Resumen cuenta corriente clientes", null, headers, rows, format, GetActiveStoreId());
    }

    [HttpGet("customers/{id}/account-statement")]
    public async Task<IActionResult> CustomerAccountStatement(int id, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string format = "xlsx")
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found" });

        var (start, endExclusive) = GetRange(from, to, 30);
        var moves = await _db.CustomerAccountMovements
            .Where(m => m.CustomerId == id && m.CreatedAt >= start && m.CreatedAt < endExclusive)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        decimal running = 0m;
        var headers = new[] { "MovementId", "CreatedAt", "Type", "ReferenceType", "ReferenceId", "Amount", "RunningBalance", "Description" };
        var rows = new List<IReadOnlyList<object?>>();
        foreach (var m in moves)
        {
            running += m.Amount;
            rows.Add(new object?[]
            {
                m.Id,
                m.CreatedAt,
                m.MovementType,
                m.ReferenceType,
                m.ReferenceId,
                m.Amount,
                running,
                m.Description
            });
        }

        return await ExportAsync("customer-account-statement", $"Estado de cuenta - {customer.FullName}", $"Desde {start:yyyy-MM-dd} hasta {endExclusive.AddSeconds(-1):yyyy-MM-dd}", headers, rows, format, GetActiveStoreId());
    }

    [HttpGet("transfers/pending")]
    public async Task<IActionResult> TransfersPending([FromQuery] string format = "xlsx")
    {
        var storeId = GetActiveStoreId();
        var sales = await _db.Sales
            .Include(s => s.Customer)
            .Where(s => s.Status == SaleStatus.PendingTransfer.ToString())
            .Where(s => !storeId.HasValue || s.StoreId == storeId.Value)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var headers = new[] { "SaleId", "StoreId", "CreatedAt", "Customer", "Total", "Invoice", "Status" };
        var rows = sales.Select(s => (IReadOnlyList<object?>)new object?[] { s.Id, s.StoreId, s.CreatedAt, s.Customer?.FullName, s.Total, s.InvoiceNumber, s.Status }).ToList();
        return await ExportAsync("transfers-pending", "Transferencias pendientes", null, headers, rows, format, storeId);
    }

    [HttpGet("containers/debtors")]
    public async Task<IActionResult> ContainerDebtors([FromQuery] string format = "xlsx")
    {
        var moves = await _db.ContainerMovements
            .Include(m => m.Customer)
            .Include(m => m.ContainerType)
            .ToListAsync();

        var debtors = moves
            .GroupBy(m => new { m.CustomerId, CustomerName = m.Customer.FullName, Container = m.ContainerType.Name })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.CustomerName,
                g.Key.Container,
                NetQty = g.Sum(x => x.Direction == ContainerDirection.Given.ToString() ? x.Qty : -x.Qty)
            })
            .Where(x => x.NetQty > 0)
            .OrderByDescending(x => x.NetQty)
            .ToList();

        var headers = new[] { "CustomerId", "Customer", "Container", "DebtQty" };
        var rows = debtors.Select(x => (IReadOnlyList<object?>)new object?[] { x.CustomerId, x.CustomerName, x.Container, x.NetQty }).ToList();
        return await ExportAsync("containers-debtors", "Deudores de envases", null, headers, rows, format, GetActiveStoreId());
    }

    [HttpGet("containers/movements")]
    public async Task<IActionResult> ContainerMovements([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string format = "xlsx")
    {
        var (start, endExclusive) = GetRange(from, to, 30);
        var rowsSource = await _db.ContainerMovements
            .Include(m => m.Customer)
            .Include(m => m.ContainerType)
            .Where(m => m.CreatedAt >= start && m.CreatedAt < endExclusive)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var headers = new[] { "MovementId", "CreatedAt", "Customer", "Container", "Direction", "Qty", "RefType", "RefId", "CreatedBy" };
        var rows = rowsSource.Select(x => (IReadOnlyList<object?>)new object?[]
        {
            x.Id,
            x.CreatedAt,
            x.Customer.FullName,
            x.ContainerType.Name,
            x.Direction,
            x.Qty,
            x.RefType,
            x.RefId,
            x.CreatedByUsuarioId
        }).ToList();

        return await ExportAsync("containers-movements", "Movimientos de envases", $"Desde {start:yyyy-MM-dd} hasta {endExclusive.AddSeconds(-1):yyyy-MM-dd}", headers, rows, format, GetActiveStoreId());
    }

    [HttpGet("stock/summary")]
    public async Task<IActionResult> StockSummary([FromQuery] string format = "xlsx")
    {
        var storeId = GetActiveStoreId();
        var stocks = await _db.ProductStocks
            .Include(ps => ps.Product)
            .Where(ps => !storeId.HasValue || ps.StoreId == storeId.Value)
            .ToListAsync();

        var summary = stocks
            .GroupBy(s => new { s.ProductId, s.Product.Name, s.Product.Barcode, s.Product.QuickCode })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                g.Key.Barcode,
                g.Key.QuickCode,
                Vendible = g.Where(x => x.Bucket == StockBucket.VENDIBLE.ToString()).Sum(x => x.Quantity),
                Reclamo = g.Where(x => x.Bucket == StockBucket.RECLAMO.ToString()).Sum(x => x.Quantity),
                Merma = g.Where(x => x.Bucket == StockBucket.MERMA.ToString()).Sum(x => x.Quantity),
                UpdatedAt = g.Max(x => x.UpdatedAt)
            })
            .OrderBy(x => x.Name)
            .ToList();

        var headers = new[] { "ProductId", "Barcode", "QuickCode", "Product", "Vendible", "Reclamo", "Merma", "UpdatedAt" };
        var rows = summary.Select(x => (IReadOnlyList<object?>)new object?[] { x.ProductId, x.Barcode, x.QuickCode, x.Name, x.Vendible, x.Reclamo, x.Merma, x.UpdatedAt }).ToList();
        return await ExportAsync("stock-summary", "Resumen de stock", null, headers, rows, format, storeId);
    }

    [HttpGet("stock/critical")]
    public async Task<IActionResult> StockCritical([FromQuery] string format = "xlsx")
    {
        var storeId = GetActiveStoreId();
        const decimal threshold = 5m;

        var critical = await _db.ProductStocks
            .Include(ps => ps.Product)
            .Where(ps => ps.Bucket == StockBucket.VENDIBLE.ToString() && ps.Quantity <= threshold && ps.Product.StockControl)
            .Where(ps => !storeId.HasValue || ps.StoreId == storeId.Value)
            .OrderBy(ps => ps.Quantity)
            .ToListAsync();

        var headers = new[] { "ProductId", "Barcode", "QuickCode", "Product", "VendibleQty", "Threshold", "StoreId" };
        var rows = critical.Select(x => (IReadOnlyList<object?>)new object?[] { x.ProductId, x.Product.Barcode, x.Product.QuickCode, x.Product.Name, x.Quantity, threshold, x.StoreId }).ToList();
        return await ExportAsync("stock-critical", "Stock critico", null, headers, rows, format, storeId);
    }

    [HttpGet("stock/movements")]
    public async Task<IActionResult> StockMovements([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string format = "xlsx")
    {
        var (start, endExclusive) = GetRange(from, to, 30);
        var storeId = GetActiveStoreId();

        var moves = await _db.StockMovements
            .Include(m => m.Product)
            .Where(m => m.CreatedAt >= start && m.CreatedAt < endExclusive)
            .Where(m => !storeId.HasValue || m.StoreId == storeId.Value)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var headers = new[] { "MovementId", "CreatedAt", "Product", "Bucket", "Type", "DeltaQty", "SaleId", "PurchaseId", "SupplierClaimId", "Notes" };
        var rows = moves.Select(m => (IReadOnlyList<object?>)new object?[]
        {
            m.Id,
            m.CreatedAt,
            m.Product.Name,
            m.Bucket,
            m.MovementType,
            m.DeltaQty,
            m.SaleId,
            m.PurchaseId,
            m.SupplierClaimId,
            m.Notes
        }).ToList();

        return await ExportAsync("stock-movements", "Movimientos de stock", $"Desde {start:yyyy-MM-dd} hasta {endExclusive.AddSeconds(-1):yyyy-MM-dd}", headers, rows, format, storeId);
    }

    [HttpGet("suppliers/claims")]
    public async Task<IActionResult> SupplierClaims([FromQuery] string format = "xlsx")
    {
        var storeId = GetActiveStoreId();
        var claims = await _db.SupplierClaims
            .Include(c => c.Supplier)
            .Include(c => c.Items)
            .Include(c => c.ExchangeLines)
            .Include(c => c.Refunds)
            .Include(c => c.Evidences)
            .Where(c => !storeId.HasValue || c.StoreId == storeId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var headers = new[] { "ClaimId", "CreatedAt", "Supplier", "Status", "SettlementRequested", "SettlementResolved", "ResolvedAt", "Items", "Qty", "ExchangeLines", "RefundAmount", "EvidenceCount", "HasReceipt", "Receipt", "Notes" };
        var rows = claims.Select(c => (IReadOnlyList<object?>)new object?[]
        {
            c.Id,
            c.CreatedAt,
            c.Supplier?.Name,
            c.Status,
            c.RequestedSettlementMode,
            c.ResolvedSettlementMode,
            c.ResolvedAt,
            c.Items.Count,
            c.Items.Sum(i => i.Quantity),
            c.ExchangeLines.Count,
            c.Refunds.Sum(r => r.Amount),
            c.Evidences.Count,
            c.HasReceipt,
            string.Join("-", new[] { c.ReceiptType, c.ReceiptNumber }.Where(x => !string.IsNullOrWhiteSpace(x))),
            c.Notes
        }).ToList();

        return await ExportAsync("suppliers-claims", "Reclamos a proveedores", null, headers, rows, format, storeId);
    }

    [HttpGet("suppliers/credits")]
    public async Task<IActionResult> SupplierCredits([FromQuery] string format = "xlsx")
    {
        var storeId = GetActiveStoreId();
        var credits = await _db.SupplierCredits
            .Include(c => c.Supplier)
            .Include(c => c.SupplierClaim)
            .Where(c => !storeId.HasValue || c.SupplierClaim == null || c.SupplierClaim.StoreId == storeId.Value)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var headers = new[] { "CreditId", "CreatedAt", "Supplier", "Amount", "Remaining", "ClaimId", "Notes" };
        var rows = credits.Select(c => (IReadOnlyList<object?>)new object?[]
        {
            c.Id,
            c.CreatedAt,
            c.Supplier?.Name,
            c.Amount,
            c.RemainingAmount,
            c.SupplierClaimId,
            c.Notes
        }).ToList();

        return await ExportAsync("suppliers-credits", "Creditos de proveedores", null, headers, rows, format, storeId);
    }

    [HttpGet("rrhh/hours")]
    public async Task<IActionResult> RrhhHours([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string format = "xlsx")
    {
        var (start, endExclusive) = GetRange(from, to, 30);

        var punches = await _db.TimePunches
            .Include(t => t.Usuario)
            .Where(t => t.PunchTime >= start && t.PunchTime < endExclusive)
            .OrderBy(t => t.PunchTime)
            .ToListAsync();

        var extras = await _db.EmployeeExtras
            .Where(e => e.ExtraDate >= start && e.ExtraDate < endExclusive)
            .ToListAsync();

        var extraMap = extras
            .GroupBy(e => new { e.UsuarioId, Date = e.ExtraDate.Date })
            .ToDictionary(g => (g.Key.UsuarioId, g.Key.Date), g => g.Sum(x => x.Hours));

        var grouped = punches
            .GroupBy(p => new { p.UsuarioId, User = p.Usuario.Username, Date = p.PunchTime.Date })
            .OrderBy(g => g.Key.Date)
            .ThenBy(g => g.Key.User)
            .ToList();

        var headers = new[] { "Date", "UsuarioId", "Usuario", "WorkedHours", "ExtraHours", "Punches" };
        var rows = grouped.Select(g =>
        {
            var worked = ComputeWorkedHours(g.OrderBy(x => x.PunchTime).ToList());
            extraMap.TryGetValue((g.Key.UsuarioId, g.Key.Date), out var extraHours);
            return (IReadOnlyList<object?>)new object?[] { g.Key.Date, g.Key.UsuarioId, g.Key.User, worked, extraHours, g.Count() };
        }).ToList();

        return await ExportAsync("rrhh-hours", "RRHH - Horas trabajadas", $"Desde {start:yyyy-MM-dd} hasta {endExclusive.AddSeconds(-1):yyyy-MM-dd}", headers, rows, format, GetActiveStoreId());
    }

    [HttpGet("rrhh/punches")]
    public async Task<IActionResult> RrhhPunches([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string format = "xlsx")
    {
        var (start, endExclusive) = GetRange(from, to, 30);
        var punches = await _db.TimePunches
            .Include(t => t.Usuario)
            .Where(t => t.PunchTime >= start && t.PunchTime < endExclusive)
            .OrderBy(t => t.PunchTime)
            .ToListAsync();

        var headers = new[] { "PunchId", "PunchTime", "UsuarioId", "Usuario", "Type", "IsOpen", "DeviceId", "CashSessionId", "IsAdjusted", "AdjustedAt" };
        var rows = punches.Select(p => (IReadOnlyList<object?>)new object?[]
        {
            p.Id,
            p.PunchTime,
            p.UsuarioId,
            p.Usuario.Username,
            p.PunchType,
            p.IsOpen,
            p.DeviceId,
            p.CashSessionId,
            p.IsAdjusted,
            p.AdjustedAt
        }).ToList();

        return await ExportAsync("rrhh-punches", "RRHH - Fichadas", $"Desde {start:yyyy-MM-dd} hasta {endExclusive.AddSeconds(-1):yyyy-MM-dd}", headers, rows, format, GetActiveStoreId());
    }

    [HttpGet("cash-sessions/{id}/summary")]
    public async Task<IActionResult> CashSessionSummary(int id, [FromQuery] string format = "pdf")
    {
        if (!string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only pdf format is supported for this endpoint" });

        var session = await _db.CashSessions.FirstOrDefaultAsync(x => x.Id == id);
        if (session == null) return NotFound();

        var movesCount = await _db.CashSessionMoneyMovements.CountAsync(m => m.CashSessionId == id);
        var salesCount = await _db.Sales.CountAsync(s => s.CashSessionId == id);
        var returnsCount = await _db.SaleReturns.CountAsync(r => r.CashSessionId == id);

        var headers = new[] { "Metric", "Value" };
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "CashSessionId", session.Id },
            new object?[] { "StoreId", session.StoreId },
            new object?[] { "Shift", session.Shift },
            new object?[] { "Status", session.Status },
            new object?[] { "OpenedAt", session.OpenedAt },
            new object?[] { "ClosedAt", session.ClosedAt },
            new object?[] { "OpeningCash", session.OpeningCash },
            new object?[] { "TotalCash", session.TotalCash },
            new object?[] { "TotalCard", session.TotalCard },
            new object?[] { "TotalTransfer", session.TotalTransfer },
            new object?[] { "TotalCredit", session.TotalCredit },
            new object?[] { "DeclaredCash", session.DeclaredCash },
            new object?[] { "DeclaredCard", session.DeclaredCard },
            new object?[] { "DeclaredTransfer", session.DeclaredTransfer },
            new object?[] { "DeclaredCredit", session.DeclaredCredit },
            new object?[] { "DiffTotal", session.DiffTotal },
            new object?[] { "SalesCount", salesCount },
            new object?[] { "ReturnsCount", returnsCount },
            new object?[] { "CashMovementsCount", movesCount },
            new object?[] { "CloseNotes", session.CloseNotes }
        };

        return await ExportAsync("cash-session-summary", $"Resumen cierre caja #{session.Id}", null, headers, rows, "pdf", session.StoreId);
    }

    [HttpGet("cash-movements")]
    public async Task<IActionResult> CashMovements([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string format = "xlsx")
    {
        var (start, endExclusive) = GetRange(from, to, 30);
        var storeId = GetActiveStoreId();

        var moves = await _db.CashSessionMoneyMovements
            .Where(m => m.CreatedAt >= start && m.CreatedAt < endExclusive)
            .Where(m => !storeId.HasValue || m.StoreId == storeId.Value)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var headers = new[] { "MovementId", "CreatedAt", "CashSessionId", "StoreId", "Method", "Type", "SignedAmount", "Reason", "Category", "RefType", "RefId" };
        var rows = moves.Select(m => (IReadOnlyList<object?>)new object?[]
        {
            m.Id,
            m.CreatedAt,
            m.CashSessionId,
            m.StoreId,
            m.Method,
            m.Type,
            m.SignedAmount,
            m.Reason,
            m.Category,
            m.RefType,
            m.RefId
        }).ToList();

        return await ExportAsync("cash-movements", "Movimientos de caja", $"Desde {start:yyyy-MM-dd} hasta {endExclusive.AddSeconds(-1):yyyy-MM-dd}", headers, rows, format, storeId);
    }

    private async Task<IActionResult> ExportAsync(string reportKey, string title, string? subtitle, IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<object?>> rows, string format, int? storeId)
    {
        var export = await _exportService.BuildAsync(reportKey, title, subtitle, headers, rows, format, storeId);
        return File(export.Content, export.ContentType, export.FileName);
    }

    private static decimal ComputeWorkedHours(IReadOnlyList<TimePunch> punches)
    {
        DateTime? open = null;
        decimal totalHours = 0m;

        foreach (var p in punches)
        {
            if (p.PunchType == TimePunchType.Entry.ToString())
            {
                open = p.PunchTime;
                continue;
            }

            if (p.PunchType == TimePunchType.Exit.ToString() && open.HasValue)
            {
                var diff = p.PunchTime - open.Value;
                if (diff.TotalMinutes > 0)
                    totalHours += (decimal)diff.TotalHours;
                open = null;
            }
        }

        return Math.Round(totalHours, 2);
    }

    private static (DateTime Start, DateTime EndExclusive) GetRange(DateTime? from, DateTime? to, int defaultDays)
    {
        var endDate = to?.Date ?? DateTime.UtcNow.Date;
        var startDate = from?.Date ?? endDate.AddDays(-(Math.Max(1, defaultDays) - 1));
        if (startDate > endDate) (startDate, endDate) = (endDate, startDate);
        return (startDate, endDate.AddDays(1));
    }

    private int? GetActiveStoreId()
    {
        var raw = Request.Headers["X-Store-Id"].FirstOrDefault();
        return int.TryParse(raw, out var id) ? id : null;
    }
}

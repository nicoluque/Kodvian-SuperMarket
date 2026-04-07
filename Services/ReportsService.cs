using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IReportsService
{
    Task<SalesDailyReportDto> GetSalesDailyAsync(DateTime date);
    Task<List<SalesRangeDayReportDto>> GetSalesRangeAsync(DateTime from, DateTime to);
    Task<List<PendingTransferReportDto>> GetPendingTransfersAsync(int? olderThanHours, int? customerId);
    Task<PendingTransferAlertsDto> GetPendingTransferAlertsAsync(int olderThanHours);
    Task<List<StockBalanceDto>> GetStockSummaryAsync();
    Task<List<StockBalanceDto>> GetStockCigarettesAsync();
    Task<List<SupplierClaimDto>> GetSupplierClaimsAsync(string? status);
    Task<List<SupplierClaimDto>> GetSupplierClaimsOverdueAsync(int days);
    Task<List<SupplierCreditBySupplierDto>> GetSupplierCreditsBySupplierAsync();
    Task<List<ContainerDebtorDto>> GetContainerDebtorsAsync(int top);
    Task<List<ContainerByCustomerDto>> GetContainersByCustomerAsync(int customerId);
    Task<List<CigaretteCountReportDto>> GetCigaretteCountsAsync(DateTime? from, DateTime? to);
    Task<List<RrhhHoursDto>> GetRrhhHoursAsync(DateTime from, DateTime to);
    Task<List<TimePunchInconsistencyDto>> GetRrhhInconsistenciesAsync(DateTime? from, DateTime? to);
}

public class ReportsService : IReportsService
{
    private readonly ApplicationDbContext _context;
    private readonly IRRHHService _rrhhService;

    public ReportsService(ApplicationDbContext context, IRRHHService rrhhService)
    {
        _context = context;
        _rrhhService = rrhhService;
    }

    public async Task<SalesDailyReportDto> GetSalesDailyAsync(DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sales = await _context.Sales
            .AsNoTracking()
            .Include(s => s.Payments)
            .Include(s => s.CashSession)
            .Where(s => s.CreatedAt >= dayStart && s.CreatedAt < dayEnd)
            .ToListAsync();

        var shifts = sales
            .GroupBy(s => new
            {
                SessionId = s.CashSessionId ?? 0,
                Shift = s.CashSession != null ? s.CashSession.Shift : "Unknown"
            })
            .Select(g => new SalesShiftReportDto
            {
                CashSessionId = g.Key.SessionId,
                Shift = g.Key.Shift,
                Total = g.Sum(x => x.Total),
                Tickets = g.Count(),
                TotalCash = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && p.PaymentMethod == PaymentMethod.Cash.ToString())
                    .Sum(p => p.Amount),
                TotalCard = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && p.PaymentMethod == PaymentMethod.Card.ToString())
                    .Sum(p => p.Amount),
                TotalTransfer = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && p.PaymentMethod == PaymentMethod.Transfer.ToString())
                    .Sum(p => p.Amount),
                TotalCredit = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && (p.PaymentMethod == PaymentMethod.Credit.ToString() || p.PaymentMethod == PaymentMethod.AccountCredit.ToString()))
                    .Sum(p => p.Amount),
                CigaretteSurcharge = g.Sum(x => x.CigaretteSurcharge),
                Discounts = g.Sum(x => x.Discount + x.PromoDiscount + x.ManualDiscount)
            })
            .OrderBy(x => x.CashSessionId)
            .ToList();

        return new SalesDailyReportDto
        {
            Date = dayStart,
            Shifts = shifts
        };
    }

    public async Task<List<SalesRangeDayReportDto>> GetSalesRangeAsync(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date.AddDays(1);

        var sales = await _context.Sales
            .AsNoTracking()
            .Include(s => s.Payments)
            .Where(s => s.CreatedAt >= start && s.CreatedAt < end)
            .ToListAsync();

        return sales
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new SalesRangeDayReportDto
            {
                Date = g.Key,
                Total = g.Sum(x => x.Total),
                Tickets = g.Count(),
                TotalCash = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && p.PaymentMethod == PaymentMethod.Cash.ToString())
                    .Sum(p => p.Amount),
                TotalCard = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && p.PaymentMethod == PaymentMethod.Card.ToString())
                    .Sum(p => p.Amount),
                TotalTransfer = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && p.PaymentMethod == PaymentMethod.Transfer.ToString())
                    .Sum(p => p.Amount),
                TotalCredit = g.SelectMany(x => x.Payments)
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString() && (p.PaymentMethod == PaymentMethod.Credit.ToString() || p.PaymentMethod == PaymentMethod.AccountCredit.ToString()))
                    .Sum(p => p.Amount),
                CigaretteSurcharge = g.Sum(x => x.CigaretteSurcharge),
                Discounts = g.Sum(x => x.Discount + x.PromoDiscount + x.ManualDiscount)
            })
            .OrderBy(x => x.Date)
            .ToList();
    }

    public async Task<List<PendingTransferReportDto>> GetPendingTransfersAsync(int? olderThanHours, int? customerId)
    {
        var query = _context.Sales
            .AsNoTracking()
            .Include(s => s.Payments)
            .Where(s => s.Status == SaleStatus.PendingTransfer.ToString())
            .AsQueryable();

        if (olderThanHours.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddHours(-olderThanHours.Value);
            query = query.Where(s => s.CreatedAt <= cutoff);
        }

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        var sales = await query
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        return sales.Select(s => new PendingTransferReportDto
        {
            SaleId = s.Id,
            CustomerId = s.CustomerId,
            CreatedAt = s.CreatedAt,
            Total = s.Total,
            PendingAmount = s.Total - s.Payments
                .Where(p => p.Status == PaymentStatus.Confirmed.ToString())
                .Sum(p => p.Amount)
        }).ToList();
    }

    public async Task<PendingTransferAlertsDto> GetPendingTransferAlertsAsync(int olderThanHours)
    {
        var threshold = olderThanHours <= 0 ? 24 : olderThanHours;
        var allPending = await _context.Sales
            .AsNoTracking()
            .Where(s => s.Status == SaleStatus.PendingTransfer.ToString())
            .ToListAsync();

        var cutoff = DateTime.UtcNow.AddHours(-threshold);
        var alert = allPending.Where(s => s.CreatedAt <= cutoff).Select(s => s.Id).ToList();

        return new PendingTransferAlertsDto
        {
            OlderThanHours = threshold,
            TotalPendingTransfers = allPending.Count,
            AlertCount = alert.Count,
            AlertSaleIds = alert
        };
    }

    public async Task<List<StockBalanceDto>> GetStockSummaryAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        var stocks = await _context.ProductStocks
            .AsNoTracking()
            .ToListAsync();

        return products.Select(p =>
        {
            var productStocks = stocks.Where(s => s.ProductId == p.Id).ToList();

            return new StockBalanceDto
            {
                ProductId = p.Id,
                ProductCode = p.Barcode,
                ProductName = p.Name,
                Vendible = productStocks.Where(x => x.Bucket == StockBucket.VENDIBLE.ToString()).Sum(x => x.Quantity),
                Reclamo = productStocks.Where(x => x.Bucket == StockBucket.RECLAMO.ToString()).Sum(x => x.Quantity),
                Merma = productStocks.Where(x => x.Bucket == StockBucket.MERMA.ToString()).Sum(x => x.Quantity)
            };
        }).ToList();
    }

    public async Task<List<StockBalanceDto>> GetStockCigarettesAsync()
    {
        var cigaretteIds = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsCigarette)
            .Select(p => p.Id)
            .ToListAsync();

        var stock = await GetStockSummaryAsync();
        return stock.Where(s => cigaretteIds.Contains(s.ProductId)).ToList();
    }

    public async Task<List<SupplierClaimDto>> GetSupplierClaimsAsync(string? status)
    {
        var query = _context.SupplierClaims
            .AsNoTracking()
            .Include(sc => sc.Supplier)
            .Include(sc => sc.Items)
                .ThenInclude(i => i.Product)
            .Include(sc => sc.Evidences)
            .Include(sc => sc.ExchangeLines)
                .ThenInclude(l => l.Product)
            .Include(sc => sc.Refunds)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(sc => sc.Status == status);

        var claims = await query
            .OrderByDescending(sc => sc.CreatedAt)
            .ToListAsync();

        return claims.Select(MapSupplierClaim).ToList();
    }

    public async Task<List<SupplierClaimDto>> GetSupplierClaimsOverdueAsync(int days)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var claims = await _context.SupplierClaims
            .AsNoTracking()
            .Include(sc => sc.Supplier)
            .Include(sc => sc.Items)
                .ThenInclude(i => i.Product)
            .Include(sc => sc.Evidences)
            .Include(sc => sc.ExchangeLines)
                .ThenInclude(l => l.Product)
            .Include(sc => sc.Refunds)
            .Where(sc => sc.CreatedAt <= cutoff && (sc.Status == SupplierClaimStatus.Pending.ToString() || sc.Status == SupplierClaimStatus.PickedUp.ToString()))
            .OrderBy(sc => sc.CreatedAt)
            .ToListAsync();

        return claims.Select(MapSupplierClaim).ToList();
    }

    public async Task<List<SupplierCreditBySupplierDto>> GetSupplierCreditsBySupplierAsync()
    {
        var credits = await _context.SupplierCredits
            .AsNoTracking()
            .Include(c => c.Supplier)
            .Where(c => c.RemainingAmount > 0)
            .ToListAsync();

        return credits
            .GroupBy(c => new
            {
                c.SupplierId,
                SupplierName = c.Supplier != null ? c.Supplier.Name : null
            })
            .Select(g => new SupplierCreditBySupplierDto
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.SupplierName,
                RemainingCredit = g.Sum(x => x.RemainingAmount)
            })
            .OrderByDescending(x => x.RemainingCredit)
            .ToList();
    }

    public async Task<List<ContainerDebtorDto>> GetContainerDebtorsAsync(int top)
    {
        var limit = top > 0 ? top : 10;

        var movements = await _context.ContainerMovements
            .AsNoTracking()
            .Include(cm => cm.Customer)
            .ToListAsync();

        return movements
            .GroupBy(cm => new { cm.CustomerId, CustomerName = cm.Customer.FullName })
            .Select(g => new ContainerDebtorDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerName,
                OwedQty = g.Sum(x => x.Direction == ContainerDirection.Given.ToString() ? x.Qty : -x.Qty)
            })
            .Where(x => x.OwedQty > 0)
            .OrderByDescending(x => x.OwedQty)
            .Take(limit)
            .ToList();
    }

    public async Task<List<ContainerByCustomerDto>> GetContainersByCustomerAsync(int customerId)
    {
        var movements = await _context.ContainerMovements
            .AsNoTracking()
            .Include(cm => cm.ContainerType)
            .Where(cm => cm.CustomerId == customerId)
            .ToListAsync();

        return movements
            .GroupBy(cm => new { cm.ContainerTypeId, cm.ContainerType.Name })
            .Select(g => new ContainerByCustomerDto
            {
                ContainerTypeId = g.Key.ContainerTypeId,
                ContainerTypeName = g.Key.Name,
                OwedQty = g.Sum(x => x.Direction == ContainerDirection.Given.ToString() ? x.Qty : -x.Qty)
            })
            .Where(x => x.OwedQty > 0)
            .OrderByDescending(x => x.OwedQty)
            .ToList();
    }

    public async Task<List<CigaretteCountReportDto>> GetCigaretteCountsAsync(DateTime? from, DateTime? to)
    {
        var query = _context.CigaretteCounts
            .AsNoTracking()
            .Include(cc => cc.CashSession)
            .Include(cc => cc.Lines)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(cc => cc.CountDate >= from.Value);

        if (to.HasValue)
            query = query.Where(cc => cc.CountDate <= to.Value);

        var counts = await query
            .OrderByDescending(cc => cc.CountDate)
            .ToListAsync();

        return counts.Select(cc => new CigaretteCountReportDto
        {
            CigaretteCountId = cc.Id,
            CashSessionId = cc.CashSessionId,
            Shift = cc.CashSession != null ? cc.CashSession.Shift : string.Empty,
            CountDate = cc.CountDate,
            TotalDiffQty = cc.Lines.Sum(l => l.DiffQty)
        }).ToList();
    }

    public async Task<List<RrhhHoursDto>> GetRrhhHoursAsync(DateTime from, DateTime to)
    {
        var punches = await _context.TimePunches
            .AsNoTracking()
            .Include(tp => tp.Usuario)
            .Where(tp => tp.PunchTime >= from && tp.PunchTime <= to)
            .OrderBy(tp => tp.UsuarioId)
            .ThenBy(tp => tp.PunchTime)
            .ToListAsync();

        var result = new List<RrhhHoursDto>();

        foreach (var userPunches in punches.GroupBy(tp => new { tp.UsuarioId, Username = tp.Usuario.Username }))
        {
            DateTime? openEntry = null;
            decimal totalHours = 0;

            foreach (var punch in userPunches)
            {
                if (punch.PunchType == TimePunchType.Entry.ToString())
                {
                    openEntry = punch.PunchTime;
                    continue;
                }

                if (punch.PunchType == TimePunchType.Exit.ToString() && openEntry.HasValue && punch.PunchTime > openEntry.Value)
                {
                    totalHours += (decimal)(punch.PunchTime - openEntry.Value).TotalHours;
                    openEntry = null;
                }
            }

            result.Add(new RrhhHoursDto
            {
                UsuarioId = userPunches.Key.UsuarioId,
                UsuarioName = userPunches.Key.Username,
                Hours = Math.Round(totalHours, 2)
            });
        }

        return result
            .OrderByDescending(x => x.Hours)
            .ThenBy(x => x.UsuarioName)
            .ToList();
    }

    public async Task<List<TimePunchInconsistencyDto>> GetRrhhInconsistenciesAsync(DateTime? from, DateTime? to)
    {
        return await _rrhhService.GetInconsistenciesAsync(from, to);
    }

    private static SupplierClaimDto MapSupplierClaim(SupplierClaim claim)
    {
        return new SupplierClaimDto
        {
            Id = claim.Id,
            SupplierId = claim.SupplierId,
            SupplierName = claim.Supplier != null ? claim.Supplier.Name : null,
            PurchaseId = claim.PurchaseId,
            Status = claim.Status,
            HasReceipt = claim.HasReceipt,
            ReceiptType = claim.ReceiptType,
            ReceiptNumber = claim.ReceiptNumber,
            Notes = claim.Notes,
            RequestedSettlementMode = claim.RequestedSettlementMode,
            ResolvedSettlementMode = claim.ResolvedSettlementMode,
            CreatedAt = claim.CreatedAt,
            PickedUpAt = claim.PickedUpAt,
            CreditedAt = claim.CreditedAt,
            ResolvedAt = claim.ResolvedAt,
            ResolvedByUserId = claim.ResolvedByUserId,
            Items = claim.Items.Select(item => new SupplierClaimItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductCode = item.Product != null ? item.Product.Barcode : null,
                ProductName = item.Product != null ? item.Product.Name : null,
                Quantity = item.Quantity,
                UnitCostSnapshot = item.UnitCostSnapshot,
                Notes = item.Notes
            }).ToList(),
            Evidences = claim.Evidences.Select(e => new SupplierClaimEvidenceDto
            {
                Id = e.Id,
                FileName = e.FileName,
                ContentType = e.ContentType,
                FileSize = e.FileSize,
                CreatedAt = e.CreatedAt,
                PreviewUrl = $"/api/v1/stock/claims/{claim.Id}/evidences/{e.Id}/preview",
                DownloadUrl = $"/api/v1/stock/claims/{claim.Id}/evidences/{e.Id}/download"
            }).ToList(),
            ExchangeLines = claim.ExchangeLines.Select(l => new SupplierClaimExchangeLineDto
            {
                Id = l.Id,
                ProductId = l.ProductId,
                ProductName = l.Product?.Name,
                Quantity = l.Quantity,
                UnitCostSnapshot = l.UnitCostSnapshot,
                Notes = l.Notes
            }).ToList(),
            Refunds = claim.Refunds.Select(r => new SupplierClaimRefundDto
            {
                Id = r.Id,
                Amount = r.Amount,
                Notes = r.Notes,
                CreatedByUserId = r.CreatedByUserId,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }
}

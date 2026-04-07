using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IPurchaseSuggestionService
{
    Task<PurchaseSuggestion> GenerateAsync(int? tenantId, int? storeId, int? generatedByUsuarioId, int daysWindow, decimal targetCoverageDays, int? supplierId, bool criticalOnly);
    Task<List<PurchaseSuggestion>> GetAllAsync(int? tenantId, int? storeId);
    Task<PurchaseSuggestion?> GetByIdAsync(int id);
    Task<PurchaseSuggestionLine> UpdateLineAsync(int suggestionId, int lineId, string? status, decimal? acceptedQty, string? notes);
    Task<List<int>> ConvertToPurchaseAsync(int suggestionId, int createdById, int deviceId);
}

public class PurchaseSuggestionService : IPurchaseSuggestionService
{
    private readonly ApplicationDbContext _db;
    private readonly IPurchaseService _purchaseService;

    public PurchaseSuggestionService(ApplicationDbContext db, IPurchaseService purchaseService)
    {
        _db = db;
        _purchaseService = purchaseService;
    }

    public async Task<PurchaseSuggestion> GenerateAsync(int? tenantId, int? storeId, int? generatedByUsuarioId, int daysWindow, decimal targetCoverageDays, int? supplierId, bool criticalOnly)
    {
        if (daysWindow <= 0) daysWindow = 14;
        if (daysWindow > 180) daysWindow = 180;
        if (targetCoverageDays <= 0) targetCoverageDays = 7;

        var start = DateTime.UtcNow.Date.AddDays(-daysWindow);
        var end = DateTime.UtcNow;

        var productsQuery = _db.Products
            .Where(p => p.StockControl && p.IsReplenishable)
            .AsQueryable();

        if (supplierId.HasValue)
            productsQuery = productsQuery.Where(p => p.PreferredSupplierId == supplierId.Value);

        var products = await productsQuery.ToListAsync();
        var productIds = products.Select(p => p.Id).ToList();

        var stockByProduct = await _db.ProductStocks
            .Where(s => s.Bucket == StockBucket.VENDIBLE.ToString())
            .Where(s => productIds.Contains(s.ProductId))
            .Where(s => !storeId.HasValue || s.StoreId == storeId.Value)
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var soldByProduct = await _db.SaleItems
            .Where(i => productIds.Contains(i.ProductId ?? -1))
            .Where(i => i.Sale.CreatedAt >= start && i.Sale.CreatedAt <= end)
            .Where(i => i.Sale.Status != SaleStatus.Cancelled.ToString() && i.Sale.Status != SaleStatus.Voided.ToString())
            .Where(i => !storeId.HasValue || i.Sale.StoreId == storeId.Value)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key!.Value, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Qty);

        var suggestion = new PurchaseSuggestion
        {
            TenantId = tenantId,
            StoreId = storeId,
            GeneratedByUsuarioId = generatedByUsuarioId,
            DaysWindow = daysWindow,
            TargetCoverageDays = targetCoverageDays,
            Status = PurchaseSuggestionStatus.Draft.ToString(),
            GeneratedAt = DateTime.UtcNow
        };

        foreach (var p in products)
        {
            var currentStock = stockByProduct.TryGetValue(p.Id, out var st) ? st : 0m;
            var totalSold = soldByProduct.TryGetValue(p.Id, out var sold) ? sold : 0m;
            var avgDailySales = Math.Round(totalSold / Math.Max(1, daysWindow), 3);
            var targetCoverageStock = Math.Round(avgDailySales * targetCoverageDays, 3);

            var belowMin = currentStock < p.MinStock;
            var belowCoverage = currentStock < targetCoverageStock;
            if (!belowMin && !belowCoverage)
                continue;

            if (criticalOnly && !belowMin)
                continue;

            var qtyByMin = Math.Max(0, p.MinStock - currentStock);
            var qtyByCoverage = Math.Max(0, targetCoverageStock - currentStock);
            var suggestedQty = Math.Max(qtyByMin, qtyByCoverage);
            if (p.ReorderQtySuggestion > 0)
                suggestedQty = Math.Max(suggestedQty, p.ReorderQtySuggestion);

            if (suggestedQty <= 0)
                continue;

            var reason = belowMin && belowCoverage
                ? "Debajo de minStock y cobertura objetivo"
                : belowMin ? "Debajo de minStock" : "Debajo de cobertura objetivo";

            suggestion.Lines.Add(new PurchaseSuggestionLine
            {
                ProductId = p.Id,
                SuggestedSupplierId = p.PreferredSupplierId,
                CurrentStock = currentStock,
                MinStock = p.MinStock,
                AvgDailySales = avgDailySales,
                TargetCoverageStock = targetCoverageStock,
                SuggestedQty = Math.Round(suggestedQty, 3),
                AcceptedQty = Math.Round(suggestedQty, 3),
                Reason = reason,
                Status = PurchaseSuggestionLineStatus.Pending.ToString()
            });
        }

        _db.PurchaseSuggestions.Add(suggestion);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(suggestion.Id) ?? suggestion;
    }

    public async Task<List<PurchaseSuggestion>> GetAllAsync(int? tenantId, int? storeId)
    {
        return await _db.PurchaseSuggestions
            .Include(s => s.Lines)
            .Where(s => !tenantId.HasValue || s.TenantId == tenantId.Value)
            .Where(s => !storeId.HasValue || s.StoreId == storeId.Value)
            .OrderByDescending(s => s.GeneratedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<PurchaseSuggestion?> GetByIdAsync(int id)
    {
        return await _db.PurchaseSuggestions
            .Include(s => s.Lines)
                .ThenInclude(l => l.Product)
            .Include(s => s.Lines)
                .ThenInclude(l => l.SuggestedSupplier)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<PurchaseSuggestionLine> UpdateLineAsync(int suggestionId, int lineId, string? status, decimal? acceptedQty, string? notes)
    {
        var line = await _db.PurchaseSuggestionLines
            .FirstOrDefaultAsync(l => l.Id == lineId && l.PurchaseSuggestionId == suggestionId);
        if (line == null)
            throw new InvalidOperationException("Suggestion line not found");

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim();
            var allowed = new[]
            {
                PurchaseSuggestionLineStatus.Pending.ToString(),
                PurchaseSuggestionLineStatus.Accepted.ToString(),
                PurchaseSuggestionLineStatus.Ignored.ToString(),
                PurchaseSuggestionLineStatus.Converted.ToString()
            };
            if (!allowed.Contains(normalized))
                throw new InvalidOperationException("Invalid line status");
            line.Status = normalized;
        }

        if (acceptedQty.HasValue)
        {
            if (acceptedQty.Value < 0)
                throw new InvalidOperationException("Accepted qty must be >= 0");
            line.AcceptedQty = Math.Round(acceptedQty.Value, 3);
        }

        if (notes != null)
            line.Notes = notes;

        await _db.SaveChangesAsync();
        return line;
    }

    public async Task<List<int>> ConvertToPurchaseAsync(int suggestionId, int createdById, int deviceId)
    {
        var suggestion = await _db.PurchaseSuggestions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == suggestionId);
        if (suggestion == null)
            throw new InvalidOperationException("Suggestion not found");

        if (suggestion.Status == PurchaseSuggestionStatus.Converted.ToString())
            throw new InvalidOperationException("Suggestion already converted");

        var lines = suggestion.Lines
            .Where(l => l.Status == PurchaseSuggestionLineStatus.Accepted.ToString() && l.AcceptedQty > 0)
            .ToList();

        if (lines.Count == 0)
            throw new InvalidOperationException("No accepted lines to convert");

        var products = await _db.Products.Where(p => lines.Select(x => x.ProductId).Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var purchaseIds = new List<int>();
        foreach (var group in lines.GroupBy(l => l.SuggestedSupplierId))
        {
            var purchase = await _purchaseService.CreateAsync(
                createdById,
                deviceId,
                group.Key,
                DocType.Order.ToString(),
                $"SUG-{suggestion.Id}",
                DateTime.UtcNow,
                group.Select(l =>
                {
                    var product = products[l.ProductId];
                    var unitCost = product.LastCost > 0 ? product.LastCost : 1m;
                    return (
                        l.ProductId,
                        l.AcceptedQty,
                        unitCost,
                        (DateTime?)null,
                        0m,
                        0m,
                        false,
                        (decimal?)null,
                        (decimal?)null
                    );
                }).ToList()
            );

            purchaseIds.Add(purchase.Id);
            foreach (var line in group)
            {
                line.Status = PurchaseSuggestionLineStatus.Converted.ToString();
                line.CreatedPurchaseId = purchase.Id;
            }
        }

        suggestion.Status = PurchaseSuggestionStatus.Converted.ToString();
        await _db.SaveChangesAsync();

        return purchaseIds;
    }
}

using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.DTOs;

namespace KodvianSuperMarket.Services;

public interface ICigaretteCountService
{
    Task<CigaretteCount> CreateCountAsync(int cashSessionId, string? notes, List<(int productId, decimal countedQty)> lines);
    Task<CigaretteCount?> GetByCashSessionIdAsync(int cashSessionId);
    Task<CigaretteCount?> GetByIdAsync(int id);
    Task<CigaretteCount> ApplyAdjustmentsAsync(int cashSessionId);
    Task<bool> HasCountAsync(int cashSessionId);
    Task<bool> HasAdjustmentsAppliedAsync(int cashSessionId);
}

public class CigaretteCountService : ICigaretteCountService
{
    private readonly ApplicationDbContext _context;
    private readonly IStockService _stockService;

    public CigaretteCountService(ApplicationDbContext context, IStockService stockService)
    {
        _context = context;
        _stockService = stockService;
    }

    public async Task<CigaretteCount> CreateCountAsync(int cashSessionId, string? notes, List<(int productId, decimal countedQty)> lines)
    {
        var existingCount = await _context.CigaretteCounts
            .FirstOrDefaultAsync(cc => cc.CashSessionId == cashSessionId);

        if (existingCount != null)
            throw new InvalidOperationException("Cigarette count already exists for this cash session");

        var cashSession = await _context.CashSessions.FindAsync(cashSessionId);
        if (cashSession == null)
            throw new InvalidOperationException("Cash session not found");

        var cigaretteCount = new CigaretteCount
        {
            CashSessionId = cashSessionId,
            CountDate = DateTime.UtcNow,
            Notes = notes,
            AdjustmentsApplied = false
        };

        _context.CigaretteCounts.Add(cigaretteCount);
        await _context.SaveChangesAsync();

        foreach (var line in lines)
        {
            var product = await _context.Products.FindAsync(line.productId);
            if (product == null)
                throw new InvalidOperationException($"Product {line.productId} not found");

            if (!product.IsCigarette)
                throw new InvalidOperationException($"Product {product.Name} is not marked as cigarette");

            var systemQty = await _stockService.GetStockBalanceAsync(line.productId, StockBucket.VENDIBLE.ToString());

            var countLine = new CigaretteCountLine
            {
                CigaretteCountId = cigaretteCount.Id,
                ProductId = line.productId,
                SystemQtyAtCount = systemQty,
                CountedQty = line.countedQty,
                AdjustmentApplied = false
            };

            _context.CigaretteCountLines.Add(countLine);
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(cigaretteCount.Id) ?? cigaretteCount;
    }

    public async Task<CigaretteCount?> GetByCashSessionIdAsync(int cashSessionId)
    {
        return await _context.CigaretteCounts
            .Include(cc => cc.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(cc => cc.CashSessionId == cashSessionId);
    }

    public async Task<CigaretteCount?> GetByIdAsync(int id)
    {
        return await _context.CigaretteCounts
            .Include(cc => cc.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(cc => cc.Id == id);
    }

    public async Task<CigaretteCount> ApplyAdjustmentsAsync(int cashSessionId)
    {
        var count = await GetByCashSessionIdAsync(cashSessionId);
        if (count == null)
            throw new InvalidOperationException("Cigarette count not found for this cash session");

        if (count.AdjustmentsApplied)
            throw new InvalidOperationException("Adjustments have already been applied");

        foreach (var line in count.Lines)
        {
            if (line.AdjustmentApplied)
                continue;

            if (line.DiffQty != 0)
            {
                await _stockService.ApplyMovementAsync(
                    line.ProductId,
                    StockBucket.VENDIBLE.ToString(),
                    line.DiffQty,
                    StockMovementType.Adjustment.ToString(),
                    notes: $"Cigarette count adjustment from session {cashSessionId}"
                );

                line.AdjustmentApplied = true;
            }
        }

        count.AdjustmentsApplied = true;
        count.AdjustmentsAppliedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return count;
    }

    public async Task<bool> HasCountAsync(int cashSessionId)
    {
        return await _context.CigaretteCounts
            .AnyAsync(cc => cc.CashSessionId == cashSessionId);
    }

    public async Task<bool> HasAdjustmentsAppliedAsync(int cashSessionId)
    {
        var count = await _context.CigaretteCounts
            .FirstOrDefaultAsync(cc => cc.CashSessionId == cashSessionId);
        return count?.AdjustmentsApplied ?? false;
    }
}

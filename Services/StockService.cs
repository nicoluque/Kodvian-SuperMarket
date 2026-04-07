using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.DTOs;

namespace KodvianSuperMarket.Services;

public interface IStockService
{
    Task<StockMovement> ApplyMovementAsync(int productId, string bucket, decimal deltaQty, string movementType, int? purchaseId = null, int? saleId = null, int? supplierClaimId = null, int? operatorSessionId = null, int? deviceId = null, string? notes = null, int? storeId = null);
    Task<ProductStock> GetOrCreateStockAsync(int productId, string bucket, int? storeId = null);
    Task<decimal> GetStockBalanceAsync(int productId, string? bucket = null);
    Task<List<ProductStockDto>> GetAllStockAsync();
    Task<List<StockMovementDto>> GetMovementsAsync(int? productId = null, string? movementType = null, DateTime? from = null, DateTime? to = null, int? storeId = null);
    Task<SupplierClaim> CreateSupplierClaimAsync(int? supplierId, int? purchaseId, bool hasReceipt, string? receiptType, string? receiptNumber, string? notes, List<(int productId, decimal quantity, decimal unitCostSnapshot, string? notes)> items, List<(string fileName, string contentType, long fileSize, byte[] fileContent)>? evidences = null, string? settlementMode = null, int? resolvedByUserId = null);
    Task<SupplierClaim?> GetSupplierClaimByIdAsync(int id);
    Task<List<SupplierClaimDto>> GetSupplierClaimsAsync(string? status = null);
    Task<SupplierClaim> PickUpSupplierClaimAsync(int id);
    Task<SupplierClaim> CreditSupplierClaimAsync(int id);
    Task<SupplierClaim> RefundSupplierClaimAsync(int id, decimal amount, string? notes = null, int? resolvedByUserId = null);
    Task<SupplierClaim> ResolveSupplierClaimExchangeAsync(int id, List<(int productId, decimal quantity, decimal unitCostSnapshot, string? notes)>? exchangeLines = null, string? notes = null, int? resolvedByUserId = null);
    Task<SupplierClaim> ReplaceSupplierClaimAsync(int id);
    Task<SupplierCredit> CreateSupplierCreditAsync(int supplierId, int? supplierClaimId, decimal amount, string? notes);
    Task<SupplierCredit?> GetSupplierCreditByIdAsync(int id);
    Task<List<SupplierCreditDto>> GetSupplierCreditsAsync(int? supplierId = null);
    Task<SupplierCredit> ApplySupplierCreditAsync(int supplierCreditId, int purchaseId, decimal appliedAmount);
    Task<List<StockBalanceDto>> GetStockReportAsync(int? storeId = null);
}

public class StockService : IStockService
{
    private readonly ApplicationDbContext _context;

    public StockService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockMovement> ApplyMovementAsync(int productId, string bucket, decimal deltaQty, string movementType, int? purchaseId = null, int? saleId = null, int? supplierClaimId = null, int? operatorSessionId = null, int? deviceId = null, string? notes = null, int? storeId = null)
    {
        return await ApplyMovementInternalAsync(productId, bucket, deltaQty, movementType, purchaseId, saleId, supplierClaimId, operatorSessionId, deviceId, notes, storeId, saveChanges: true);
    }

    private async Task<StockMovement> ApplyMovementInternalAsync(int productId, string bucket, decimal deltaQty, string movementType, int? purchaseId = null, int? saleId = null, int? supplierClaimId = null, int? operatorSessionId = null, int? deviceId = null, string? notes = null, int? storeId = null, bool saveChanges = true)
    {
        if (!storeId.HasValue && deviceId.HasValue)
            storeId = await _context.Devices.Where(d => d.Id == deviceId.Value).Select(d => d.StoreId).FirstOrDefaultAsync();

        var stock = await GetOrCreateStockInternalAsync(productId, bucket, storeId, saveChanges: false);
        stock.Quantity += deltaQty;
        stock.UpdatedAt = DateTime.UtcNow;

        var movement = new StockMovement
        {
            ProductId = productId,
            Bucket = bucket,
            DeltaQty = deltaQty,
            MovementType = movementType,
            PurchaseId = purchaseId,
            SaleId = saleId,
            SupplierClaimId = supplierClaimId,
            OperatorSessionId = operatorSessionId,
            DeviceId = deviceId,
            StoreId = storeId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.StockMovements.Add(movement);
        if (saveChanges)
            await _context.SaveChangesAsync();

        return movement;
    }

    public async Task<ProductStock> GetOrCreateStockAsync(int productId, string bucket, int? storeId = null)
    {
        return await GetOrCreateStockInternalAsync(productId, bucket, storeId, saveChanges: true);
    }

    private async Task<ProductStock> GetOrCreateStockInternalAsync(int productId, string bucket, int? storeId = null, bool saveChanges = true)
    {
        var stock = await _context.ProductStocks
            .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Bucket == bucket && ps.StoreId == storeId);

        if (stock == null)
        {
            stock = new ProductStock
            {
                ProductId = productId,
                Bucket = bucket,
                StoreId = storeId,
                Quantity = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProductStocks.Add(stock);
            if (saveChanges)
                await _context.SaveChangesAsync();
        }

        return stock;
    }

    public async Task<decimal> GetStockBalanceAsync(int productId, string? bucket = null)
    {
        if (bucket != null)
        {
            var stock = await _context.ProductStocks
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Bucket == bucket);
            return stock?.Quantity ?? 0;
        }

        var stocks = await _context.ProductStocks
            .Where(ps => ps.ProductId == productId)
            .ToListAsync();
        return stocks.Sum(ps => ps.Quantity);
    }

    public async Task<List<ProductStockDto>> GetAllStockAsync()
    {
        return await _context.ProductStocks
            .Include(ps => ps.Product)
            .OrderBy(ps => ps.Product!.Name)
            .Select(ps => new ProductStockDto
            {
                Id = ps.Id,
                ProductId = ps.ProductId,
                ProductCode = ps.Product!.Barcode,
                ProductName = ps.Product.Name,
                Bucket = ps.Bucket,
                Quantity = ps.Quantity,
                UpdatedAt = ps.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<List<StockMovementDto>> GetMovementsAsync(int? productId = null, string? movementType = null, DateTime? from = null, DateTime? to = null, int? storeId = null)
    {
        var query = _context.StockMovements
            .Include(sm => sm.Product)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(sm => sm.ProductId == productId.Value);

        if (!string.IsNullOrEmpty(movementType))
            query = query.Where(sm => sm.MovementType == movementType);

        if (from.HasValue)
            query = query.Where(sm => sm.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(sm => sm.CreatedAt <= to.Value);

        if (storeId.HasValue)
            query = query.Where(sm => sm.StoreId == storeId.Value || sm.StoreId == null);

        return await query
            .OrderByDescending(sm => sm.CreatedAt)
            .Select(sm => new StockMovementDto
            {
                Id = sm.Id,
                ProductId = sm.ProductId,
                ProductCode = sm.Product!.Barcode,
                ProductName = sm.Product.Name,
                Bucket = sm.Bucket,
                DeltaQty = sm.DeltaQty,
                MovementType = sm.MovementType,
                PurchaseId = sm.PurchaseId,
                SaleId = sm.SaleId,
                SupplierClaimId = sm.SupplierClaimId,
                OperatorSessionId = sm.OperatorSessionId,
                DeviceId = sm.DeviceId,
                Notes = sm.Notes,
                CreatedAt = sm.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SupplierClaim> CreateSupplierClaimAsync(int? supplierId, int? purchaseId, bool hasReceipt, string? receiptType, string? receiptNumber, string? notes, List<(int productId, decimal quantity, decimal unitCostSnapshot, string? notes)> items, List<(string fileName, string contentType, long fileSize, byte[] fileContent)>? evidences = null, string? settlementMode = null, int? resolvedByUserId = null)
    {
        var (requestedSettlementMode, _) = await ResolveSettlementModeAsync(supplierId, settlementMode);

        var claim = new SupplierClaim
        {
            SupplierId = supplierId,
            PurchaseId = purchaseId,
            Status = SupplierClaimStatus.Pending.ToString(),
            HasReceipt = hasReceipt,
            ReceiptType = receiptType,
            ReceiptNumber = receiptNumber,
            Notes = notes,
            RequestedSettlementMode = requestedSettlementMode,
            CreatedAt = DateTime.UtcNow
        };

        _context.SupplierClaims.Add(claim);
        await _context.SaveChangesAsync();

        foreach (var item in items)
        {
            var claimItem = new SupplierClaimItem
            {
                SupplierClaimId = claim.Id,
                ProductId = item.productId,
                Quantity = item.quantity,
                UnitCostSnapshot = item.unitCostSnapshot,
                Notes = item.notes
            };
            _context.SupplierClaimItems.Add(claimItem);

            if (item.quantity > 0)
            {
                await ApplyMovementInternalAsync(
                    item.productId,
                    StockBucket.RECLAMO.ToString(),
                    item.quantity,
                    StockMovementType.SupplierClaim.ToString(),
                    purchaseId: purchaseId,
                    supplierClaimId: claim.Id,
                    notes: $"Supplier claim created: {notes}",
                    saveChanges: false
                );
            }
        }

        if (evidences != null)
        {
            foreach (var evidence in evidences)
            {
                _context.SupplierClaimEvidences.Add(new SupplierClaimEvidence
                {
                    SupplierClaimId = claim.Id,
                    FileName = evidence.fileName,
                    ContentType = evidence.contentType,
                    FileSize = evidence.fileSize,
                    FileContent = evidence.fileContent,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return claim;
    }

    public async Task<SupplierClaim?> GetSupplierClaimByIdAsync(int id)
    {
        return await _context.SupplierClaims
            .Include(sc => sc.Items)
            .ThenInclude(sci => sci.Product)
            .Include(sc => sc.Evidences)
            .Include(sc => sc.ExchangeLines)
            .ThenInclude(l => l.Product)
            .Include(sc => sc.Refunds)
            .Include(sc => sc.Supplier)
            .FirstOrDefaultAsync(sc => sc.Id == id);
    }

    public async Task<List<SupplierClaimDto>> GetSupplierClaimsAsync(string? status = null)
    {
        var query = _context.SupplierClaims
            .Include(sc => sc.Items)
            .ThenInclude(sci => sci.Product)
            .Include(sc => sc.Evidences)
            .Include(sc => sc.ExchangeLines)
            .ThenInclude(l => l.Product)
            .Include(sc => sc.Refunds)
            .Include(sc => sc.Supplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(sc => sc.Status == status);

        return await query
            .OrderByDescending(sc => sc.CreatedAt)
            .Select(sc => new SupplierClaimDto
            {
                Id = sc.Id,
                SupplierId = sc.SupplierId,
                SupplierName = sc.Supplier != null ? sc.Supplier.Name : null,
                PurchaseId = sc.PurchaseId,
                Status = sc.Status,
                HasReceipt = sc.HasReceipt,
                ReceiptType = sc.ReceiptType,
                ReceiptNumber = sc.ReceiptNumber,
                Notes = sc.Notes,
                RequestedSettlementMode = sc.RequestedSettlementMode,
                ResolvedSettlementMode = sc.ResolvedSettlementMode,
                CreatedAt = sc.CreatedAt,
                PickedUpAt = sc.PickedUpAt,
                CreditedAt = sc.CreditedAt,
                ResolvedAt = sc.ResolvedAt,
                ResolvedByUserId = sc.ResolvedByUserId,
                Items = sc.Items.Select(sci => new SupplierClaimItemDto
                {
                    Id = sci.Id,
                    ProductId = sci.ProductId,
                    ProductCode = sci.Product != null ? sci.Product.Barcode : null,
                    ProductName = sci.Product != null ? sci.Product.Name : null,
                    Quantity = sci.Quantity,
                    UnitCostSnapshot = sci.UnitCostSnapshot,
                    Notes = sci.Notes
                }).ToList(),
                Evidences = sc.Evidences
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => new SupplierClaimEvidenceDto
                    {
                        Id = e.Id,
                        FileName = e.FileName,
                        ContentType = e.ContentType,
                        FileSize = e.FileSize,
                        CreatedAt = e.CreatedAt,
                        PreviewUrl = $"/api/v1/stock/claims/{sc.Id}/evidences/{e.Id}/preview",
                        DownloadUrl = $"/api/v1/stock/claims/{sc.Id}/evidences/{e.Id}/download"
                    })
                    .ToList(),
                ExchangeLines = sc.ExchangeLines.Select(l => new SupplierClaimExchangeLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    ProductName = l.Product != null ? l.Product.Name : null,
                    Quantity = l.Quantity,
                    UnitCostSnapshot = l.UnitCostSnapshot,
                    Notes = l.Notes
                })
                    .ToList(),
                Refunds = sc.Refunds.Select(r => new SupplierClaimRefundDto
                {
                    Id = r.Id,
                    Amount = r.Amount,
                    Notes = r.Notes,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedAt = r.CreatedAt
                })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<SupplierClaim> PickUpSupplierClaimAsync(int id)
    {
        var claim = await _context.SupplierClaims.FindAsync(id);
        if (claim == null)
            throw new InvalidOperationException("Supplier claim not found");

        if (claim.Status != SupplierClaimStatus.Pending.ToString())
            throw new InvalidOperationException("Only pending claims can be picked up");

        claim.Status = SupplierClaimStatus.PickedUp.ToString();
        claim.PickedUpAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return claim;
    }

    public async Task<SupplierClaim> CreditSupplierClaimAsync(int id)
    {
        var claim = await _context.SupplierClaims
            .Include(sc => sc.Items)
            .FirstOrDefaultAsync(sc => sc.Id == id);

        if (claim == null)
            throw new InvalidOperationException("Supplier claim not found");

        if (claim.Status != SupplierClaimStatus.PickedUp.ToString())
            throw new InvalidOperationException("Only picked up claims can be credited");

        if (!IsClaimSettlementMode(claim.RequestedSettlementMode, SupplierClaimSettlementMode.Credit))
            throw new InvalidOperationException("Este reclamo no está configurado para crédito");

        var totalAmount = claim.Items.Sum(i => i.Quantity * i.UnitCostSnapshot);

        var credit = await CreateSupplierCreditAsync(
            claim.SupplierId ?? 0,
            claim.Id,
            totalAmount,
            $"Credit for claim #{claim.Id}"
        );

        claim.Status = SupplierClaimStatus.Credited.ToString();
        claim.CreditedAt = DateTime.UtcNow;
        claim.ResolvedAt = claim.CreditedAt;
        claim.ResolvedSettlementMode = SupplierClaimSettlementMode.Credit.ToString();

        foreach (var item in claim.Items)
        {
            await ApplyMovementInternalAsync(
                item.ProductId,
                StockBucket.RECLAMO.ToString(),
                -item.Quantity,
                StockMovementType.SupplierClaim.ToString(),
                supplierClaimId: claim.Id,
                notes: "Claim credited, stock removed from RECLAMO bucket",
                saveChanges: false
            );
        }

        await _context.SaveChangesAsync();
        return claim;
    }

    public async Task<SupplierClaim> ReplaceSupplierClaimAsync(int id)
    {
        return await ResolveSupplierClaimExchangeAsync(id, null, "Reposición física", null);
    }

    public async Task<SupplierClaim> RefundSupplierClaimAsync(int id, decimal amount, string? notes = null, int? resolvedByUserId = null)
    {
        var claim = await _context.SupplierClaims
            .Include(sc => sc.Items)
            .FirstOrDefaultAsync(sc => sc.Id == id);

        if (claim == null)
            throw new InvalidOperationException("Supplier claim not found");

        if (claim.Status != SupplierClaimStatus.PickedUp.ToString())
            throw new InvalidOperationException("Only picked up claims can be refunded");

        if (!IsClaimSettlementMode(claim.RequestedSettlementMode, SupplierClaimSettlementMode.Refund))
            throw new InvalidOperationException("Este reclamo no está configurado para reembolso");

        var totalAmount = claim.Items.Sum(i => i.Quantity * i.UnitCostSnapshot);
        var refundAmount = amount > 0 ? amount : totalAmount;
        if (refundAmount <= 0)
            throw new InvalidOperationException("El monto de reembolso debe ser mayor a 0");

        foreach (var item in claim.Items)
        {
            await ApplyMovementInternalAsync(
                item.ProductId,
                StockBucket.RECLAMO.ToString(),
                -item.Quantity,
                StockMovementType.SupplierClaim.ToString(),
                supplierClaimId: claim.Id,
                notes: "Claim refunded, stock removed from RECLAMO bucket",
                saveChanges: false
            );
        }

        _context.SupplierClaimRefunds.Add(new SupplierClaimRefund
        {
            SupplierClaimId = claim.Id,
            Amount = refundAmount,
            Notes = notes,
            CreatedByUserId = resolvedByUserId,
            CreatedAt = DateTime.UtcNow
        });

        claim.Status = SupplierClaimStatus.Refunded.ToString();
        claim.CreditedAt = DateTime.UtcNow;
        claim.ResolvedAt = claim.CreditedAt;
        claim.ResolvedByUserId = resolvedByUserId;
        claim.ResolvedSettlementMode = SupplierClaimSettlementMode.Refund.ToString();

        await _context.SaveChangesAsync();
        return claim;
    }

    public async Task<SupplierClaim> ResolveSupplierClaimExchangeAsync(int id, List<(int productId, decimal quantity, decimal unitCostSnapshot, string? notes)>? exchangeLines = null, string? notes = null, int? resolvedByUserId = null)
    {
        var claim = await _context.SupplierClaims
            .Include(sc => sc.Items)
            .Include(sc => sc.ExchangeLines)
            .FirstOrDefaultAsync(sc => sc.Id == id);

        if (claim == null)
            throw new InvalidOperationException("Supplier claim not found");

        if (claim.Status != SupplierClaimStatus.PickedUp.ToString())
            throw new InvalidOperationException("Only picked up claims can be replaced");

        if (!IsClaimSettlementMode(claim.RequestedSettlementMode, SupplierClaimSettlementMode.ExchangeGoods))
            throw new InvalidOperationException("Este reclamo no está configurado para reposición con mercadería");

        var resolvedLines = (exchangeLines != null && exchangeLines.Count > 0)
            ? exchangeLines.Where(l => l.productId > 0 && l.quantity > 0).ToList()
            : claim.Items.Select(i => (productId: i.ProductId, quantity: i.Quantity, unitCostSnapshot: i.UnitCostSnapshot, notes: i.Notes)).ToList();

        if (resolvedLines.Count == 0)
            throw new InvalidOperationException("Debe informar al menos una línea de reposición");

        foreach (var item in claim.Items)
        {
            await ApplyMovementInternalAsync(
                item.ProductId,
                StockBucket.RECLAMO.ToString(),
                -item.Quantity,
                StockMovementType.SupplierClaim.ToString(),
                supplierClaimId: claim.Id,
                notes: "Claim resolved with replacement goods, removed from RECLAMO bucket",
                saveChanges: false
            );
        }

        if (claim.ExchangeLines.Count > 0)
            _context.SupplierClaimExchangeLines.RemoveRange(claim.ExchangeLines);

        foreach (var line in resolvedLines)
        {
            _context.SupplierClaimExchangeLines.Add(new SupplierClaimExchangeLine
            {
                SupplierClaimId = claim.Id,
                ProductId = line.productId,
                Quantity = line.quantity,
                UnitCostSnapshot = line.unitCostSnapshot,
                Notes = line.notes
            });

            await ApplyMovementInternalAsync(
                line.productId,
                StockBucket.VENDIBLE.ToString(),
                line.quantity,
                StockMovementType.SupplierClaim.ToString(),
                supplierClaimId: claim.Id,
                notes: "Claim resolved with replacement goods, added to VENDIBLE bucket",
                saveChanges: false
            );
        }

        claim.Status = SupplierClaimStatus.Replaced.ToString();
        claim.CreditedAt = DateTime.UtcNow;
        claim.ResolvedAt = claim.CreditedAt;
        claim.ResolvedByUserId = resolvedByUserId;
        claim.ResolvedSettlementMode = SupplierClaimSettlementMode.ExchangeGoods.ToString();
        if (!string.IsNullOrWhiteSpace(notes))
            claim.Notes = string.IsNullOrWhiteSpace(claim.Notes) ? notes : $"{claim.Notes}. {notes}";

        await _context.SaveChangesAsync();
        return claim;
    }

    public async Task<SupplierCredit> CreateSupplierCreditAsync(int supplierId, int? supplierClaimId, decimal amount, string? notes)
    {
        var credit = new SupplierCredit
        {
            SupplierId = supplierId > 0 ? supplierId : null,
            SupplierClaimId = supplierClaimId,
            Amount = amount,
            RemainingAmount = amount,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.SupplierCredits.Add(credit);
        await _context.SaveChangesAsync();
        return credit;
    }

    public async Task<SupplierCredit?> GetSupplierCreditByIdAsync(int id)
    {
        return await _context.SupplierCredits
            .Include(sc => sc.Supplier)
            .FirstOrDefaultAsync(sc => sc.Id == id);
    }

    public async Task<List<SupplierCreditDto>> GetSupplierCreditsAsync(int? supplierId = null)
    {
        var query = _context.SupplierCredits
            .Include(sc => sc.Supplier)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(sc => sc.SupplierId == supplierId.Value);

        var credits = await query
            .OrderByDescending(sc => sc.CreatedAt)
            .ToListAsync();

        var result = new List<SupplierCreditDto>();
        foreach (var credit in credits)
        {
            var applications = await _context.SupplierCreditApplications
                .Include(sca => sca.Purchase)
                .Where(sca => sca.SupplierCreditId == credit.Id)
                .ToListAsync();

            result.Add(new SupplierCreditDto
            {
                Id = credit.Id,
                SupplierId = credit.SupplierId,
                SupplierName = credit.Supplier?.Name,
                Amount = credit.Amount,
                RemainingAmount = credit.RemainingAmount,
                SupplierClaimId = credit.SupplierClaimId,
                Notes = credit.Notes,
                CreatedAt = credit.CreatedAt,
                Applications = applications.Select(a => new SupplierCreditApplicationDto
                {
                    Id = a.Id,
                    PurchaseId = a.PurchaseId,
                    PurchaseNumber = a.Purchase?.DocNumber,
                    AppliedAmount = a.AppliedAmount,
                    CreatedAt = a.CreatedAt
                }).ToList()
            });
        }

        return result;
    }

    public async Task<SupplierCredit> ApplySupplierCreditAsync(int supplierCreditId, int purchaseId, decimal appliedAmount)
    {
        var credit = await _context.SupplierCredits.FindAsync(supplierCreditId);
        if (credit == null)
            throw new InvalidOperationException("Supplier credit not found");

        var existingApplication = await _context.SupplierCreditApplications
            .FirstOrDefaultAsync(a => a.SupplierCreditId == supplierCreditId && a.PurchaseId == purchaseId && a.AppliedAmount == appliedAmount);
        if (existingApplication != null)
            return credit;

        if (appliedAmount > credit.RemainingAmount)
            throw new InvalidOperationException("Applied amount exceeds remaining credit");

        var purchase = await _context.Purchases.FindAsync(purchaseId);
        if (purchase == null)
            throw new InvalidOperationException("Purchase not found");

        var application = new SupplierCreditApplication
        {
            PurchaseId = purchaseId,
            SupplierCreditId = supplierCreditId,
            AppliedAmount = appliedAmount,
            CreatedAt = DateTime.UtcNow
        };

        _context.SupplierCreditApplications.Add(application);

        credit.RemainingAmount -= appliedAmount;

        await _context.SaveChangesAsync();
        return credit;
    }

    public async Task<List<StockBalanceDto>> GetStockReportAsync(int? storeId = null)
    {
        var products = await _context.Products
            .Where(p => p.StockControl || _context.ProductStocks.Any(ps => ps.ProductId == p.Id))
            .ToListAsync();

        var balances = new List<StockBalanceDto>();

        foreach (var product in products)
        {
            var stocksQuery = _context.ProductStocks
                .Where(ps => ps.ProductId == product.Id);

            if (storeId.HasValue)
                stocksQuery = stocksQuery.Where(ps => ps.StoreId == storeId.Value || ps.StoreId == null);

            var stocks = await stocksQuery
                .ToListAsync();

            balances.Add(new StockBalanceDto
            {
                ProductId = product.Id,
                ProductCode = product.Barcode,
                ProductName = product.Name,
                Vendible = stocks.Where(ps => ps.Bucket == StockBucket.VENDIBLE.ToString()).Sum(ps => ps.Quantity),
                Reclamo = stocks.Where(ps => ps.Bucket == StockBucket.RECLAMO.ToString()).Sum(ps => ps.Quantity),
                Merma = stocks.Where(ps => ps.Bucket == StockBucket.MERMA.ToString()).Sum(ps => ps.Quantity)
            });
        }

        return balances;
    }

    private static bool IsClaimSettlementMode(string? value, SupplierClaimSettlementMode expected)
    {
        if (!Enum.TryParse<SupplierClaimSettlementMode>(value, true, out var parsed))
            return false;
        return parsed == expected;
    }

    private async Task<(string mode, bool canOverride)> ResolveSettlementModeAsync(int? supplierId, string? requestedMode)
    {
        if (!supplierId.HasValue)
        {
            var normalizedNoSupplier = NormalizeSettlementModeOrDefault(requestedMode);
            return (normalizedNoSupplier, true);
        }

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId.Value);
        if (supplier == null)
            throw new InvalidOperationException("Supplier not found");

        var supplierMode = NormalizeSettlementModeOrDefault(supplier.ClaimSettlementModeDefault);
        if (string.IsNullOrWhiteSpace(requestedMode))
            return (supplierMode, supplier.AllowClaimSettlementOverride);

        var requested = NormalizeSettlementModeOrDefault(requestedMode);
        if (requested == supplierMode)
            return (requested, supplier.AllowClaimSettlementOverride);

        if (!supplier.AllowClaimSettlementOverride)
            throw new InvalidOperationException("Este proveedor no permite cambiar la condición de resolución de reclamos");

        return (requested, true);
    }

    private static string NormalizeSettlementModeOrDefault(string? mode)
    {
        var value = (mode ?? SupplierClaimSettlementMode.Credit.ToString()).Trim();
        if (Enum.TryParse<SupplierClaimSettlementMode>(value, true, out var parsed))
            return parsed.ToString();
        throw new InvalidOperationException("SettlementMode no es válido");
    }
}

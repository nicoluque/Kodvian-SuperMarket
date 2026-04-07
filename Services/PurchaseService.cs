using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IPurchaseService
{
    Task<Purchase> CreateAsync(int createdById, int deviceId, int? supplierId, string docType, string? docNumber, DateTime purchaseDate, List<(int productId, decimal quantity, decimal unitCost, DateTime? expiryDate, decimal damagedForClaimQty, decimal discardQty, bool updateSalePrice, decimal? newSalePrice, decimal? newPricePerKg)> items);
    Task<Purchase?> GetByIdAsync(int id);
    Task<List<Purchase>> GetAllAsync(string? status = null);
    Task<Purchase> UpdateAsync(int id, int? supplierId, string? docType, string? docNumber, DateTime? purchaseDate, List<(int productId, decimal quantity, decimal unitCost, DateTime? expiryDate, decimal damagedForClaimQty, decimal discardQty, bool updateSalePrice, decimal? newSalePrice, decimal? newPricePerKg)>? items);
    Task<Purchase> ConfirmAsync(int id);
    Task<Purchase> CancelAsync(int id, string reason);
    Task<Supplier> CreateSupplierAsync(string name, string? cuit, string? address, string? phone, string? email, string? claimSettlementModeDefault, bool allowClaimSettlementOverride);
    Task<Supplier> UpdateSupplierAsync(int id, string? name, string? cuit, string? address, string? phone, string? email, bool? isActive, string? claimSettlementModeDefault, bool? allowClaimSettlementOverride);
    Task<List<Supplier>> GetSuppliersAsync();
    Task<SupplierReturn> CreateSupplierReturnAsync(int supplierId, int deviceId, int operatorSessionId, int usuarioId, DateTime? date, List<(int productId, decimal qty)> lines);
    Task<ExternalExchange> CreateExternalExchangeAsync(int supplierId, int deviceId, int operatorSessionId, int usuarioId, DateTime? date, List<(string direction, int productId, decimal qty)> lines);
}

public class PurchaseService : IPurchaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IStockService _stockService;

    public PurchaseService(ApplicationDbContext context, IStockService stockService)
    {
        _context = context;
        _stockService = stockService;
    }

    public async Task<Purchase> CreateAsync(int createdById, int deviceId, int? supplierId, string docType, string? docNumber, DateTime purchaseDate, List<(int productId, decimal quantity, decimal unitCost, DateTime? expiryDate, decimal damagedForClaimQty, decimal discardQty, bool updateSalePrice, decimal? newSalePrice, decimal? newPricePerKg)> items)
    {
        var subtotal = items.Sum(i => i.quantity * i.unitCost);
        var tax = 0m;
        var total = subtotal + tax;
        var storeId = await _context.Devices.Where(d => d.Id == deviceId).Select(d => d.StoreId).FirstOrDefaultAsync();

        var purchase = new Purchase
        {
            SupplierId = supplierId,
            DocType = docType,
            DocNumber = docNumber,
            PurchaseDate = purchaseDate,
            CreatedById = createdById,
            DeviceId = deviceId,
            StoreId = storeId,
            Status = PurchaseStatus.Draft.ToString(),
            Subtotal = subtotal,
            Tax = tax,
            Total = total,
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        foreach (var item in items)
        {
            var product = await _context.Products.FindAsync(item.productId);
            if (product == null)
                throw new InvalidOperationException($"Product {item.productId} not found");

            if (item.expiryDate.HasValue && !product.TracksExpiry)
                throw new InvalidOperationException($"El producto {product.Name} no maneja vencimiento. Deja la fecha vacía.");

            var purchaseItem = new PurchaseItem
            {
                PurchaseId = purchase.Id,
                ProductId = item.productId,
                Quantity = item.quantity,
                UnitCost = item.unitCost,
                ExpiryDate = item.expiryDate,
                DamagedForClaimQty = item.damagedForClaimQty,
                DiscardQty = item.discardQty,
                UpdateSalePrice = item.updateSalePrice,
                NewSalePrice = item.newSalePrice,
                NewPricePerKg = item.newPricePerKg,
                CreatedAt = DateTime.UtcNow
            };
            _context.PurchaseItems.Add(purchaseItem);
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(purchase.Id) ?? purchase;
    }

    public async Task<Purchase?> GetByIdAsync(int id)
    {
        return await _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Purchase>> GetAllAsync(string? status = null)
    {
        var query = _context.Purchases
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Purchase> UpdateAsync(int id, int? supplierId, string? docType, string? docNumber, DateTime? purchaseDate, List<(int productId, decimal quantity, decimal unitCost, DateTime? expiryDate, decimal damagedForClaimQty, decimal discardQty, bool updateSalePrice, decimal? newSalePrice, decimal? newPricePerKg)>? items)
    {
        var purchase = await _context.Purchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchase == null)
            throw new InvalidOperationException("Purchase not found");

        if (purchase.Status != PurchaseStatus.Draft.ToString())
            throw new InvalidOperationException("Only draft purchases can be updated");

        if (supplierId.HasValue) purchase.SupplierId = supplierId;
        if (docType != null) purchase.DocType = docType;
        if (docNumber != null) purchase.DocNumber = docNumber;
        if (purchaseDate.HasValue) purchase.PurchaseDate = purchaseDate.Value;

        if (items != null)
        {
            foreach (var existingItem in purchase.Items.ToList())
            {
                _context.PurchaseItems.Remove(existingItem);
            }

            var subtotal = 0m;
            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.productId);
                if (product == null)
                    throw new InvalidOperationException($"Product {item.productId} not found");

                if (item.expiryDate.HasValue && !product.TracksExpiry)
                    throw new InvalidOperationException($"El producto {product.Name} no maneja vencimiento. Deja la fecha vacía.");

                subtotal += item.quantity * item.unitCost;

                _context.PurchaseItems.Add(new PurchaseItem
                {
                    PurchaseId = purchase.Id,
                    ProductId = item.productId,
                    Quantity = item.quantity,
                    UnitCost = item.unitCost,
                    ExpiryDate = item.expiryDate,
                    DamagedForClaimQty = item.damagedForClaimQty,
                    DiscardQty = item.discardQty,
                    UpdateSalePrice = item.updateSalePrice,
                    NewSalePrice = item.newSalePrice,
                    NewPricePerKg = item.newPricePerKg,
                    CreatedAt = DateTime.UtcNow
                });
            }

            purchase.Subtotal = subtotal;
            purchase.Total = subtotal + purchase.Tax;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(purchase.Id) ?? purchase;
    }

    public async Task<Purchase> ConfirmAsync(int id)
    {
        var purchase = await _context.Purchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchase == null)
            throw new InvalidOperationException("Purchase not found");

        if (purchase.Status != PurchaseStatus.Draft.ToString())
            throw new InvalidOperationException("Only draft purchases can be confirmed");

        if (!purchase.Items.Any())
            throw new InvalidOperationException("Purchase must have at least one item");

        foreach (var item in purchase.Items)
        {
            if (item.DamagedForClaimQty + item.DiscardQty > item.Quantity)
                throw new InvalidOperationException($"Damaged + Discarded quantity cannot exceed total quantity for product {item.ProductId}");

            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product {item.ProductId} not found");

            if (product.StockControl)
            {
                var netQty = item.Quantity - item.DamagedForClaimQty - item.DiscardQty;

                if (netQty > 0)
                {
                    await _stockService.ApplyMovementAsync(
                        item.ProductId,
                        StockBucket.VENDIBLE.ToString(),
                        netQty,
                        StockMovementType.Purchase.ToString(),
                        purchaseId: purchase.Id,
                        deviceId: purchase.DeviceId,
                        notes: $"Purchase {purchase.DocNumber}"
                    );
                }

                if (item.DamagedForClaimQty > 0)
                {
                    await _stockService.ApplyMovementAsync(
                        item.ProductId,
                        StockBucket.RECLAMO.ToString(),
                        item.DamagedForClaimQty,
                        StockMovementType.Purchase.ToString(),
                        purchaseId: purchase.Id,
                        deviceId: purchase.DeviceId,
                        notes: $"Damaged for claim from purchase {purchase.DocNumber}"
                    );
                }

                if (item.DiscardQty > 0)
                {
                    await _stockService.ApplyMovementAsync(
                        item.ProductId,
                        StockBucket.MERMA.ToString(),
                        item.DiscardQty,
                        StockMovementType.Waste.ToString(),
                        purchaseId: purchase.Id,
                        deviceId: purchase.DeviceId,
                        notes: $"Discarded from purchase {purchase.DocNumber}"
                    );
                }
            }

            product.LastCost = item.UnitCost;
            product.LastCostUpdatedAt = DateTime.UtcNow;

            if (item.UpdateSalePrice)
            {
                var generalList = await _context.PriceLists.FirstOrDefaultAsync(pl => pl.IsDefault);
                if (generalList != null)
                {
                    var existingPrice = await _context.ProductPrices
                        .FirstOrDefaultAsync(pp => pp.ProductId == item.ProductId && pp.PriceListId == generalList.Id);

                    if (existingPrice != null)
                    {
                        if (item.NewSalePrice.HasValue)
                            existingPrice.Price = item.NewSalePrice.Value;
                        if (item.NewPricePerKg.HasValue)
                            existingPrice.PricePerKg = item.NewPricePerKg.Value;
                    }
                    else
                    {
                        _context.ProductPrices.Add(new ProductPrice
                        {
                            ProductId = item.ProductId,
                            PriceListId = generalList.Id,
                            Price = item.NewSalePrice ?? product.DefaultPrice,
                            PricePerKg = item.NewPricePerKg ?? product.DefaultPricePerKg
                        });
                    }
                }
            }
        }

        purchase.Status = PurchaseStatus.Confirmed.ToString();
        purchase.ConfirmedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(purchase.Id) ?? purchase;
    }

    public async Task<Purchase> CancelAsync(int id, string reason)
    {
        var purchase = await _context.Purchases.FindAsync(id);

        if (purchase == null)
            throw new InvalidOperationException("Purchase not found");

        if (purchase.Status == PurchaseStatus.Cancelled.ToString())
            throw new InvalidOperationException("Purchase is already cancelled");

        if (purchase.Status == PurchaseStatus.Confirmed.ToString())
            throw new InvalidOperationException("Confirmed purchases cannot be cancelled");

        purchase.Status = PurchaseStatus.Cancelled.ToString();
        purchase.CancelReason = reason;
        purchase.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(purchase.Id) ?? purchase;
    }

    public async Task<Supplier> CreateSupplierAsync(string name, string? cuit, string? address, string? phone, string? email, string? claimSettlementModeDefault, bool allowClaimSettlementOverride)
    {
        var settlementMode = NormalizeSettlementModeOrThrow(claimSettlementModeDefault);
        var supplier = new Supplier
        {
            Name = name,
            CUIT = cuit,
            Address = address,
            Phone = phone,
            Email = email,
            IsActive = true,
            ClaimSettlementModeDefault = settlementMode,
            AllowClaimSettlementOverride = allowClaimSettlementOverride,
            CreatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier> UpdateSupplierAsync(int id, string? name, string? cuit, string? address, string? phone, string? email, bool? isActive, string? claimSettlementModeDefault, bool? allowClaimSettlementOverride)
    {
        var supplier = await _context.Suppliers.FindAsync(id)
            ?? throw new InvalidOperationException("Supplier not found");

        if (!string.IsNullOrWhiteSpace(name)) supplier.Name = name.Trim();
        if (cuit != null) supplier.CUIT = cuit;
        if (address != null) supplier.Address = address;
        if (phone != null) supplier.Phone = phone;
        if (email != null) supplier.Email = email;
        if (isActive.HasValue) supplier.IsActive = isActive.Value;
        if (!string.IsNullOrWhiteSpace(claimSettlementModeDefault))
            supplier.ClaimSettlementModeDefault = NormalizeSettlementModeOrThrow(claimSettlementModeDefault);
        if (allowClaimSettlementOverride.HasValue)
            supplier.AllowClaimSettlementOverride = allowClaimSettlementOverride.Value;

        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        return await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<SupplierReturn> CreateSupplierReturnAsync(int supplierId, int deviceId, int operatorSessionId, int usuarioId, DateTime? date, List<(int productId, decimal qty)> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("At least one line is required");

        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null)
            throw new InvalidOperationException("Supplier not found");

        var entity = new SupplierReturn
        {
            SupplierId = supplierId,
            ReturnDate = date ?? DateTime.UtcNow,
            DeviceId = deviceId,
            CreatedByOperatorSessionId = operatorSessionId,
            CreatedByUsuarioId = usuarioId,
            CreatedAt = DateTime.UtcNow
        };

        _context.SupplierReturns.Add(entity);
        await _context.SaveChangesAsync();

        foreach (var line in lines)
        {
            if (line.qty <= 0)
                throw new InvalidOperationException("Qty must be > 0");

            var product = await _context.Products.FindAsync(line.productId);
            if (product == null)
                throw new InvalidOperationException($"Product {line.productId} not found");

            _context.SupplierReturnLines.Add(new SupplierReturnLine
            {
                SupplierReturnId = entity.Id,
                ProductId = product.Id,
                Qty = line.qty,
                UnitCostSnapshot = product.LastCost
            });

            await _stockService.ApplyMovementAsync(
                product.Id,
                StockBucket.VENDIBLE.ToString(),
                -line.qty,
                "SUPPLIER_RETURN_OUT",
                operatorSessionId: operatorSessionId,
                deviceId: deviceId,
                notes: $"Supplier return #{entity.Id}"
            );
        }

        await _context.SaveChangesAsync();

        return await _context.SupplierReturns
            .Include(r => r.Lines)
            .FirstAsync(r => r.Id == entity.Id);
    }

    public async Task<ExternalExchange> CreateExternalExchangeAsync(int supplierId, int deviceId, int operatorSessionId, int usuarioId, DateTime? date, List<(string direction, int productId, decimal qty)> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("At least one line is required");

        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null)
            throw new InvalidOperationException("Supplier not found");

        var entity = new ExternalExchange
        {
            SupplierId = supplierId,
            ExchangeDate = date ?? DateTime.UtcNow,
            DeviceId = deviceId,
            CreatedByOperatorSessionId = operatorSessionId,
            CreatedByUsuarioId = usuarioId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExternalExchanges.Add(entity);
        await _context.SaveChangesAsync();

        foreach (var line in lines)
        {
            if (line.qty <= 0)
                throw new InvalidOperationException("Qty must be > 0");

            if (line.direction != ExternalExchangeDirection.Give.ToString() && line.direction != ExternalExchangeDirection.Receive.ToString())
                throw new InvalidOperationException("Invalid direction");

            var product = await _context.Products.FindAsync(line.productId);
            if (product == null)
                throw new InvalidOperationException($"Product {line.productId} not found");

            _context.ExternalExchangeLines.Add(new ExternalExchangeLine
            {
                ExternalExchangeId = entity.Id,
                Direction = line.direction,
                ProductId = line.productId,
                Qty = line.qty
            });

            var delta = line.direction == ExternalExchangeDirection.Give.ToString() ? -line.qty : line.qty;
            var movementType = line.direction == ExternalExchangeDirection.Give.ToString() ? "EXCHANGE_GIVE_OUT" : "EXCHANGE_RECEIVE_IN";

            await _stockService.ApplyMovementAsync(
                line.productId,
                StockBucket.VENDIBLE.ToString(),
                delta,
                movementType,
                operatorSessionId: operatorSessionId,
                deviceId: deviceId,
                notes: $"External exchange #{entity.Id}"
            );
        }

        await _context.SaveChangesAsync();

        return await _context.ExternalExchanges
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == entity.Id);
    }

    private static string NormalizeSettlementModeOrThrow(string? mode)
    {
        var value = (mode ?? SupplierClaimSettlementMode.Credit.ToString()).Trim();
        if (Enum.TryParse<SupplierClaimSettlementMode>(value, true, out var parsed))
            return parsed.ToString();

        throw new InvalidOperationException("ClaimSettlementModeDefault no es válido");
    }
}

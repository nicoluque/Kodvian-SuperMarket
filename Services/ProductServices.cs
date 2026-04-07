using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IProductService
{
    Task<Product> CreateAsync(string name, string? barcode, string? quickCode, string saleType, bool isCigarette, bool allowsManualPrice, bool tracksExpiry, bool stockControl, int? containerTypeId, decimal? containerDepositOverride, string? unitName, decimal defaultPrice, decimal defaultPricePerKg);
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> GetAllAsync(string? status = null);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product?> GetByQuickCodeAsync(string quickCode);
    Task<List<Product>> GetPendingAsync();
    Task<Product> UpdateAsync(int id, string? name, string? barcode, string? quickCode, string? saleType, bool? isCigarette, bool? allowsManualPrice, bool? tracksExpiry, bool? stockControl, int? containerTypeId, decimal? containerDepositOverride, string? catalogStatus, string? unitName, decimal? defaultPrice, decimal? defaultPricePerKg);
    Task<Product> ActivateAsync(int id);
    Task<decimal> GetPriceAsync(int productId, int? priceListId);
}

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product> CreateAsync(string name, string? barcode, string? quickCode, string saleType, bool isCigarette, bool allowsManualPrice, bool tracksExpiry, bool stockControl, int? containerTypeId, decimal? containerDepositOverride, string? unitName, decimal defaultPrice, decimal defaultPricePerKg)
    {
        var product = new Product
        {
            Name = name,
            Barcode = barcode,
            QuickCode = quickCode,
            SaleType = saleType,
            IsCigarette = isCigarette,
            AllowsManualPrice = allowsManualPrice,
            TracksExpiry = tracksExpiry,
            StockControl = stockControl,
            ContainerTypeId = containerTypeId,
            ContainerDepositOverride = containerDepositOverride,
            CatalogStatus = CatalogStatus.Pending.ToString(),
            UnitName = unitName,
            DefaultPrice = defaultPrice,
            DefaultPricePerKg = defaultPricePerKg,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<List<Product>> GetAllAsync(string? status = null)
    {
        var query = _context.Products.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.CatalogStatus == status);
        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    public async Task<Product?> GetByQuickCodeAsync(string quickCode)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.QuickCode == quickCode);
    }

    public async Task<List<Product>> GetPendingAsync()
    {
        return await _context.Products
            .Where(p => p.CatalogStatus == CatalogStatus.Pending.ToString())
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Product> UpdateAsync(int id, string? name, string? barcode, string? quickCode, string? saleType, bool? isCigarette, bool? allowsManualPrice, bool? tracksExpiry, bool? stockControl, int? containerTypeId, decimal? containerDepositOverride, string? catalogStatus, string? unitName, decimal? defaultPrice, decimal? defaultPricePerKg)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new InvalidOperationException("Product not found");

        if (name != null) product.Name = name;
        if (barcode != null) product.Barcode = barcode;
        if (quickCode != null) product.QuickCode = quickCode;
        if (saleType != null) product.SaleType = saleType;
        if (isCigarette.HasValue) product.IsCigarette = isCigarette.Value;
        if (allowsManualPrice.HasValue) product.AllowsManualPrice = allowsManualPrice.Value;
        if (tracksExpiry.HasValue) product.TracksExpiry = tracksExpiry.Value;
        if (stockControl.HasValue) product.StockControl = stockControl.Value;
        product.ContainerTypeId = containerTypeId;
        product.ContainerDepositOverride = containerDepositOverride;
        if (catalogStatus != null) product.CatalogStatus = catalogStatus;
        if (unitName != null) product.UnitName = unitName;
        if (defaultPrice.HasValue) product.DefaultPrice = defaultPrice.Value;
        if (defaultPricePerKg.HasValue) product.DefaultPricePerKg = defaultPricePerKg.Value;
        
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<Product> ActivateAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new InvalidOperationException("Product not found");

        product.CatalogStatus = CatalogStatus.Active.ToString();
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<decimal> GetPriceAsync(int productId, int? priceListId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            throw new InvalidOperationException("Product not found");

        if (priceListId.HasValue)
        {
            var price = await _context.ProductPrices
                .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.PriceListId == priceListId.Value);
            if (price != null)
                return price.Price;
        }

        var defaultList = await _context.PriceLists.FirstOrDefaultAsync(pl => pl.IsDefault);
        if (defaultList != null)
        {
            var price = await _context.ProductPrices
                .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.PriceListId == defaultList.Id);
            if (price != null)
                return price.Price;
        }

        return product.DefaultPrice;
    }
}

public interface IPriceListService
{
    Task<PriceList> CreateAsync(string name, bool isDefault, bool isActive);
    Task<PriceList?> GetByIdAsync(int id);
    Task<List<PriceList>> GetAllAsync();
    Task<PriceList> UpdateAsync(int id, string? name, bool? isDefault, bool? isActive);
    Task BulkUpdatePricesAsync(int priceListId, List<(int productId, decimal price, decimal pricePerKg)> prices);
}

public class PriceListService : IPriceListService
{
    private readonly ApplicationDbContext _context;

    public PriceListService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PriceList> CreateAsync(string name, bool isDefault, bool isActive)
    {
        if (isDefault)
        {
            var existing = await _context.PriceLists.FirstOrDefaultAsync(pl => pl.IsDefault);
            if (existing != null)
                existing.IsDefault = false;
        }

        var priceList = new PriceList
        {
            Name = name,
            IsDefault = isDefault,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();

        return priceList;
    }

    public async Task<PriceList?> GetByIdAsync(int id)
    {
        return await _context.PriceLists.FindAsync(id);
    }

    public async Task<List<PriceList>> GetAllAsync()
    {
        return await _context.PriceLists.OrderBy(pl => pl.Name).ToListAsync();
    }

    public async Task<PriceList> UpdateAsync(int id, string? name, bool? isDefault, bool? isActive)
    {
        var priceList = await _context.PriceLists.FindAsync(id);
        if (priceList == null)
            throw new InvalidOperationException("PriceList not found");

        if (isDefault == true && !priceList.IsDefault)
        {
            var existing = await _context.PriceLists.FirstOrDefaultAsync(pl => pl.IsDefault && pl.Id != id);
            if (existing != null)
                existing.IsDefault = false;
            priceList.IsDefault = true;
        }

        if (name != null) priceList.Name = name;
        if (isActive.HasValue) priceList.IsActive = isActive.Value;
        priceList.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return priceList;
    }

    public async Task BulkUpdatePricesAsync(int priceListId, List<(int productId, decimal price, decimal pricePerKg)> prices)
    {
        foreach (var (productId, price, pricePerKg) in prices)
        {
            var existing = await _context.ProductPrices
                .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.PriceListId == priceListId);

            if (existing != null)
            {
                existing.Price = price;
                existing.PricePerKg = pricePerKg;
            }
            else
            {
                _context.ProductPrices.Add(new ProductPrice
                {
                    ProductId = productId,
                    PriceListId = priceListId,
                    Price = price,
                    PricePerKg = pricePerKg
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}

public interface IPromotionService
{
    Task<Promotion> CreateAsync(string name, string promotionType, int? nxmBuyQty, int? nxmFreeQty, decimal? percentDiscount, decimal? packPrice, decimal? minPurchaseAmount, int priority, DateTime startDate, DateTime endDate, bool isActive, List<int> productIds);
    Task<Promotion?> GetByIdAsync(int id);
    Task<List<Promotion>> GetAllAsync();
    Task<List<Promotion>> GetActiveAsync();
    Task<Promotion> UpdateAsync(int id, string? name, string? promotionType, int? nxmBuyQty, int? nxmFreeQty, decimal? percentDiscount, decimal? packPrice, decimal? minPurchaseAmount, int? priority, DateTime? startDate, DateTime? endDate, bool? isActive, List<int>? productIds);
    Task<Promotion?> GetBestPromotionAsync(int productId, decimal quantity, decimal currentSubtotal);
}

public class PromotionService : IPromotionService
{
    private readonly ApplicationDbContext _context;

    public PromotionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Promotion> CreateAsync(string name, string promotionType, int? nxmBuyQty, int? nxmFreeQty, decimal? percentDiscount, decimal? packPrice, decimal? minPurchaseAmount, int priority, DateTime startDate, DateTime endDate, bool isActive, List<int> productIds)
    {
        var promotion = new Promotion
        {
            Name = name,
            PromotionType = promotionType,
            NxM_BuyQuantity = nxmBuyQty,
            NxM_FreeQuantity = nxmFreeQty,
            PercentDiscount = percentDiscount,
            PackPrice = packPrice,
            MinPurchaseAmount = minPurchaseAmount,
            Priority = priority,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Promotions.Add(promotion);
        await _context.SaveChangesAsync();

        foreach (var productId in productIds)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null && product.IsCigarette)
                throw new InvalidOperationException("Cigarettes cannot be associated with promotions");

            _context.PromotionProducts.Add(new PromotionProduct
            {
                PromotionId = promotion.Id,
                ProductId = productId
            });
        }

        await _context.SaveChangesAsync();
        return promotion;
    }

    public async Task<Promotion?> GetByIdAsync(int id)
    {
        return await _context.Promotions
            .Include(p => p.Products)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Promotion>> GetAllAsync()
    {
        return await _context.Promotions
            .Include(p => p.Products)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    public async Task<List<Promotion>> GetActiveAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Promotions
            .Include(p => p.Products)
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    public async Task<Promotion> UpdateAsync(int id, string? name, string? promotionType, int? nxmBuyQty, int? nxmFreeQty, decimal? percentDiscount, decimal? packPrice, decimal? minPurchaseAmount, int? priority, DateTime? startDate, DateTime? endDate, bool? isActive, List<int>? productIds)
    {
        var promotion = await _context.Promotions
            .Include(p => p.Products)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (promotion == null)
            throw new InvalidOperationException("Promotion not found");

        if (name != null) promotion.Name = name;
        if (promotionType != null) promotion.PromotionType = promotionType;
        if (nxmBuyQty.HasValue) promotion.NxM_BuyQuantity = nxmBuyQty;
        if (nxmFreeQty.HasValue) promotion.NxM_FreeQuantity = nxmFreeQty;
        if (percentDiscount.HasValue) promotion.PercentDiscount = percentDiscount;
        if (packPrice.HasValue) promotion.PackPrice = packPrice;
        if (minPurchaseAmount.HasValue) promotion.MinPurchaseAmount = minPurchaseAmount;
        if (priority.HasValue) promotion.Priority = priority.Value;
        if (startDate.HasValue) promotion.StartDate = startDate.Value;
        if (endDate.HasValue) promotion.EndDate = endDate.Value;
        if (isActive.HasValue) promotion.IsActive = isActive.Value;

        if (productIds != null)
        {
            foreach (var pp in promotion.Products.ToList())
            {
                _context.PromotionProducts.Remove(pp);
            }

            foreach (var productId in productIds)
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null && product.IsCigarette)
                    throw new InvalidOperationException("Cigarettes cannot be associated with promotions");

                _context.PromotionProducts.Add(new PromotionProduct
                {
                    PromotionId = promotion.Id,
                    ProductId = productId
                });
            }
        }

        await _context.SaveChangesAsync();
        return promotion;
    }

    public async Task<Promotion?> GetBestPromotionAsync(int productId, decimal quantity, decimal currentSubtotal)
    {
        var now = DateTime.UtcNow;
        var promotions = await _context.Promotions
            .Include(p => p.Products)
            .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();

        return promotions.FirstOrDefault(p => 
            p.Products.Any(pp => pp.ProductId == productId) &&
            (!p.MinPurchaseAmount.HasValue || currentSubtotal >= p.MinPurchaseAmount.Value));
    }
}

public interface ISettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<Dictionary<string, string>> GetPOSSettingsAsync();
    Task UpdatePOSSettingsAsync(decimal? bigPurchaseMinAmount, decimal? bigPurchaseDiscountCapPercent, decimal? cigaretteSurchargePercent, List<string>? cigaretteSurchargeMethods, bool? lateFeeEnabled, decimal? lateFeePercentMonthly);
}

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;

    public SettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetAsync(string key)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting != null)
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.Settings.Add(new Setting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
        }
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetPOSSettingsAsync()
    {
        var settings = await _context.Settings.ToListAsync();
        return settings.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task UpdatePOSSettingsAsync(decimal? bigPurchaseMinAmount, decimal? bigPurchaseDiscountCapPercent, decimal? cigaretteSurchargePercent, List<string>? cigaretteSurchargeMethods, bool? lateFeeEnabled, decimal? lateFeePercentMonthly)
    {
        if (bigPurchaseMinAmount.HasValue)
            await SetAsync("BigPurchaseMinAmount", bigPurchaseMinAmount.Value.ToString());
        if (bigPurchaseDiscountCapPercent.HasValue)
            await SetAsync("BigPurchaseDiscountCapPercent", bigPurchaseDiscountCapPercent.Value.ToString());
        if (cigaretteSurchargePercent.HasValue)
            await SetAsync("CigaretteSurchargePercent", cigaretteSurchargePercent.Value.ToString());
        if (cigaretteSurchargeMethods != null)
            await SetAsync("CigaretteSurchargeMethods", string.Join(",", cigaretteSurchargeMethods));
        if (lateFeeEnabled.HasValue)
            await SetAsync("LateFeeEnabled", lateFeeEnabled.Value ? "true" : "false");
        if (lateFeePercentMonthly.HasValue)
            await SetAsync("LateFeePercentMonthly", lateFeePercentMonthly.Value.ToString());
    }
}

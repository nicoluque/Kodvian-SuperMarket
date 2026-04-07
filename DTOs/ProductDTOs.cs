using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class ProductCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QuickCode { get; set; }
    public string SaleType { get; set; } = "Unit";
    public bool IsCigarette { get; set; } = false;
    public bool AllowsManualPrice { get; set; } = false;
    public bool TracksExpiry { get; set; } = false;
    public bool StockControl { get; set; } = false;
    public int? ContainerTypeId { get; set; }
    public decimal? ContainerDepositOverride { get; set; }
    public string? UnitName { get; set; }
    public decimal DefaultPrice { get; set; } = 0;
    public decimal DefaultPricePerKg { get; set; } = 0;
}

public class ProductUpdateRequest
{
    public string? Name { get; set; }
    public string? Barcode { get; set; }
    public string? QuickCode { get; set; }
    public string? SaleType { get; set; }
    public bool? IsCigarette { get; set; }
    public bool? AllowsManualPrice { get; set; }
    public bool? TracksExpiry { get; set; }
    public bool? StockControl { get; set; }
    public int? ContainerTypeId { get; set; }
    public decimal? ContainerDepositOverride { get; set; }
    public string? CatalogStatus { get; set; }
    public string? UnitName { get; set; }
    public decimal? DefaultPrice { get; set; }
    public decimal? DefaultPricePerKg { get; set; }
}

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? QuickCode { get; set; }
    public string SaleType { get; set; } = string.Empty;
    public bool IsCigarette { get; set; }
    public bool AllowsManualPrice { get; set; }
    public bool TracksExpiry { get; set; }
    public bool StockControl { get; set; }
    public int? ContainerTypeId { get; set; }
    public decimal? ContainerDepositOverride { get; set; }
    public string CatalogStatus { get; set; } = string.Empty;
    public string? UnitName { get; set; }
    public decimal DefaultPrice { get; set; }
    public decimal DefaultPricePerKg { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PriceListCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class PriceListUpdateRequest
{
    public string? Name { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsActive { get; set; }
}

public class PriceListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductPriceRequest
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    public decimal Price { get; set; }
    public decimal PricePerKg { get; set; }
}

public class BulkPriceUpdateRequest
{
    [Required]
    public int PriceListId { get; set; }
    [Required]
    public List<ProductPriceRequest> Prices { get; set; } = new();
}

public class PromotionCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string PromotionType { get; set; } = "Percent";
    public int? NxM_BuyQuantity { get; set; }
    public int? NxM_FreeQuantity { get; set; }
    public decimal? PercentDiscount { get; set; }
    public decimal? PackPrice { get; set; }
    public decimal? MinPurchaseAmount { get; set; }
    public int Priority { get; set; } = 0;
    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> ProductIds { get; set; } = new();
}

public class PromotionUpdateRequest
{
    public string? Name { get; set; }
    public string? PromotionType { get; set; }
    public int? NxM_BuyQuantity { get; set; }
    public int? NxM_FreeQuantity { get; set; }
    public decimal? PercentDiscount { get; set; }
    public decimal? PackPrice { get; set; }
    public decimal? MinPurchaseAmount { get; set; }
    public int? Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsActive { get; set; }
    public List<int>? ProductIds { get; set; }
}

public class PromotionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PromotionType { get; set; } = string.Empty;
    public int? NxM_BuyQuantity { get; set; }
    public int? NxM_FreeQuantity { get; set; }
    public decimal? PercentDiscount { get; set; }
    public decimal? PackPrice { get; set; }
    public decimal? MinPurchaseAmount { get; set; }
    public int Priority { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public List<int> ProductIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SettingsResponse
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class POSSettingsRequest
{
    public decimal? BigPurchaseMinAmount { get; set; }
    public decimal? BigPurchaseDiscountCapPercent { get; set; }
    public decimal? CigaretteSurchargePercent { get; set; }
    public List<string>? CigaretteSurchargeMethods { get; set; }
    public bool? LateFeeEnabled { get; set; }
    public decimal? LateFeePercentMonthly { get; set; }
}

public class ContainerTypeCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public decimal DepositAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ContainerTypeUpdateRequest
{
    public string? Name { get; set; }
    public decimal? DepositAmount { get; set; }
    public bool? IsActive { get; set; }
}

public class ContainerTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

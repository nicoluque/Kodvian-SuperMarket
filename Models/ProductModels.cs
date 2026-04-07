using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum SaleType
{
    Unit,
    Weight,
    Package
}

public enum CatalogStatus
{
    Pending,
    Active,
    Inactive
}

public class ContainerType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal DepositAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Barcode { get; set; }

    [MaxLength(20)]
    public string? QuickCode { get; set; }

    [Required]
    [MaxLength(20)]
    public string SaleType { get; set; } = "Unit";

    public bool IsCigarette { get; set; } = false;

    public bool AllowsManualPrice { get; set; } = false;

    public bool TracksExpiry { get; set; } = false;

    public bool StockControl { get; set; } = false;

    [Column(TypeName = "decimal(12,3)")]
    public decimal MinStock { get; set; } = 0;

    [Column(TypeName = "decimal(12,3)")]
    public decimal ReorderQtySuggestion { get; set; } = 0;

    public int? PreferredSupplierId { get; set; }

    [ForeignKey(nameof(PreferredSupplierId))]
    public Supplier? PreferredSupplier { get; set; }

    [MaxLength(20)]
    public string PurchaseUnit { get; set; } = "Unit";

    public bool IsReplenishable { get; set; } = true;

    public int? ContainerTypeId { get; set; }

    [ForeignKey(nameof(ContainerTypeId))]
    public ContainerType? ContainerType { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? ContainerDepositOverride { get; set; }

    [Required]
    [MaxLength(20)]
    public string CatalogStatus { get; set; } = "Pending";

    [MaxLength(20)]
    public string? UnitName { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal DefaultPrice { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    public decimal DefaultPricePerKg { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    public decimal LastCost { get; set; } = 0;

    public DateTime? LastCostUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductPrice> Prices { get; set; } = new List<ProductPrice>();
    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}

public class PriceList
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();
}

public class ProductPrice
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    public int PriceListId { get; set; }

    [ForeignKey(nameof(PriceListId))]
    public PriceList PriceList { get; set; } = null!;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal PricePerKg { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum PromotionType
{
    NxM,
    Percent,
    PackPrice
}

public class Promotion
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PromotionType { get; set; } = "Percent";

    public int? NxM_BuyQuantity { get; set; }

    public int? NxM_FreeQuantity { get; set; }

    public decimal? PercentDiscount { get; set; }

    public decimal? PackPrice { get; set; }

    public decimal? MinPurchaseAmount { get; set; }

    public int Priority { get; set; } = 0;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PromotionProduct> Products { get; set; } = new List<PromotionProduct>();
}

public class PromotionProduct
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PromotionId { get; set; }

    [ForeignKey(nameof(PromotionId))]
    public Promotion Promotion { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;
}

public class Setting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

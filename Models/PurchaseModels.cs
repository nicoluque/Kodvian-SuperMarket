using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CUIT { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(30)]
    public string ClaimSettlementModeDefault { get; set; } = SupplierClaimSettlementMode.Credit.ToString();

    public bool AllowClaimSettlementOverride { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum SupplierClaimSettlementMode
{
    Credit,
    Refund,
    ExchangeGoods
}

public enum PurchaseStatus
{
    Draft,
    Confirmed,
    Cancelled
}

public enum DocType
{
    Invoice,
    Receipt,
    Remit,
    Order
}

public enum ExternalExchangeDirection
{
    Give,
    Receive
}

public class Purchase
{
    [Key]
    public int Id { get; set; }

    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [Required]
    [MaxLength(20)]
    public string DocType { get; set; } = "Invoice";

    [MaxLength(50)]
    public string? DocNumber { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public Usuario CreatedBy { get; set; } = null!;

    public int DeviceId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = PurchaseStatus.Draft.ToString();

    [Column(TypeName = "decimal(12,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Tax { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Total { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}

public class PurchaseItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PurchaseId { get; set; }

    [ForeignKey(nameof(PurchaseId))]
    public Purchase Purchase { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitCost { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public decimal DamagedForClaimQty { get; set; } = 0;

    public decimal DiscardQty { get; set; } = 0;

    public decimal VendibleQty => Quantity - DamagedForClaimQty - DiscardQty;

    public bool UpdateSalePrice { get; set; } = false;

    [Column(TypeName = "decimal(12,2)")]
    public decimal? NewSalePrice { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? NewPricePerKg { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierReturn
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier Supplier { get; set; } = null!;

    public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

    public int DeviceId { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;

    public int CreatedByOperatorSessionId { get; set; }

    [ForeignKey(nameof(CreatedByOperatorSessionId))]
    public OperatorSession CreatedByOperatorSession { get; set; } = null!;

    public int CreatedByUsuarioId { get; set; }

    [ForeignKey(nameof(CreatedByUsuarioId))]
    public Usuario CreatedByUsuario { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplierReturnLine> Lines { get; set; } = new List<SupplierReturnLine>();
}

public class SupplierReturnLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierReturnId { get; set; }

    [ForeignKey(nameof(SupplierReturnId))]
    public SupplierReturn SupplierReturn { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Column(TypeName = "decimal(12,3)")]
    public decimal Qty { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitCostSnapshot { get; set; }
}

public class ExternalExchange
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier Supplier { get; set; } = null!;

    public DateTime ExchangeDate { get; set; } = DateTime.UtcNow;

    public int DeviceId { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;

    public int CreatedByOperatorSessionId { get; set; }

    [ForeignKey(nameof(CreatedByOperatorSessionId))]
    public OperatorSession CreatedByOperatorSession { get; set; } = null!;

    public int CreatedByUsuarioId { get; set; }

    [ForeignKey(nameof(CreatedByUsuarioId))]
    public Usuario CreatedByUsuario { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExternalExchangeLine> Lines { get; set; } = new List<ExternalExchangeLine>();
}

public class ExternalExchangeLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ExternalExchangeId { get; set; }

    [ForeignKey(nameof(ExternalExchangeId))]
    public ExternalExchange ExternalExchange { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Direction { get; set; } = ExternalExchangeDirection.Give.ToString();

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Column(TypeName = "decimal(12,3)")]
    public decimal Qty { get; set; }
}

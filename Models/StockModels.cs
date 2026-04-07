using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum StockBucket
{
    VENDIBLE,
    RECLAMO,
    MERMA
}

public enum StockMovementType
{
    Purchase,
    Sale,
    SupplierClaim,
    Waste,
    Adjustment,
    Transformation,
    Initial
}

public enum StockCountSessionType
{
    OpeningBalance,
    CycleCount
}

public enum StockCountSessionStatus
{
    Draft,
    Previewed,
    Committed,
    Cancelled
}

public class ProductStock
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Bucket { get; set; } = StockBucket.VENDIBLE.ToString();

    [Column(TypeName = "decimal(12,3)")]
    public decimal Quantity { get; set; } = 0;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class StockMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Bucket { get; set; } = StockBucket.VENDIBLE.ToString();

    [Required]
    [Column(TypeName = "decimal(12,3)")]
    public decimal DeltaQty { get; set; }

    [Required]
    [MaxLength(30)]
    public string MovementType { get; set; } = StockMovementType.Initial.ToString();

    public int? PurchaseId { get; set; }

    [ForeignKey(nameof(PurchaseId))]
    public Purchase? Purchase { get; set; }

    public int? SaleId { get; set; }

    [ForeignKey(nameof(SaleId))]
    public Sale? Sale { get; set; }

    public int? SupplierClaimId { get; set; }

    public int? OperatorSessionId { get; set; }

    public int? DeviceId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StockCountSession
{
    [Key]
    public int Id { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [Required]
    [MaxLength(30)]
    public string SessionType { get; set; } = StockCountSessionType.OpeningBalance.ToString();

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = StockCountSessionStatus.Draft.ToString();

    public bool ExplicitConfirmation { get; set; } = false;

    [MaxLength(500)]
    public string? WarningMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CommittedAt { get; set; }

    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}

public class StockCountLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StockCountSessionId { get; set; }

    [ForeignKey(nameof(StockCountSessionId))]
    public StockCountSession StockCountSession { get; set; } = null!;

    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [MaxLength(50)]
    public string? Barcode { get; set; }

    [MaxLength(20)]
    public string? QuickCode { get; set; }

    [MaxLength(200)]
    public string? ProductName { get; set; }

    public int RowNumber { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal CurrentVendibleQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal CurrentReclamoQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal CurrentMermaQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal TargetVendibleQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal TargetReclamoQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal TargetMermaQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal DeltaVendibleQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal DeltaReclamoQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal DeltaMermaQty { get; set; }

    [MaxLength(500)]
    public string? Error { get; set; }
}

public enum SupplierClaimStatus
{
    Pending,
    PickedUp,
    Credited,
    Replaced,
    Refunded
}

public class SupplierClaim
{
    [Key]
    public int Id { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    public int? PurchaseId { get; set; }

    [ForeignKey(nameof(PurchaseId))]
    public Purchase? Purchase { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = SupplierClaimStatus.Pending.ToString();

    public bool HasReceipt { get; set; } = false;

    [MaxLength(50)]
    public string? ReceiptType { get; set; }

    [MaxLength(50)]
    public string? ReceiptNumber { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(30)]
    public string RequestedSettlementMode { get; set; } = SupplierClaimSettlementMode.Credit.ToString();

    [MaxLength(30)]
    public string? ResolvedSettlementMode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PickedUpAt { get; set; }

    public DateTime? CreditedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public int? ResolvedByUserId { get; set; }

    public ICollection<SupplierClaimItem> Items { get; set; } = new List<SupplierClaimItem>();

    public ICollection<SupplierClaimEvidence> Evidences { get; set; } = new List<SupplierClaimEvidence>();

    public ICollection<SupplierClaimExchangeLine> ExchangeLines { get; set; } = new List<SupplierClaimExchangeLine>();

    public ICollection<SupplierClaimRefund> Refunds { get; set; } = new List<SupplierClaimRefund>();
}

public class SupplierClaimItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierClaimId { get; set; }

    [ForeignKey(nameof(SupplierClaimId))]
    public SupplierClaim SupplierClaim { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(12,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitCostSnapshot { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SupplierClaimEvidence
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierClaimId { get; set; }

    [ForeignKey(nameof(SupplierClaimId))]
    public SupplierClaim SupplierClaim { get; set; } = null!;

    [Required]
    [MaxLength(180)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    [Required]
    public long FileSize { get; set; }

    [Required]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierClaimExchangeLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierClaimId { get; set; }

    [ForeignKey(nameof(SupplierClaimId))]
    public SupplierClaim SupplierClaim { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(12,3)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal UnitCostSnapshot { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SupplierClaimRefund
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SupplierClaimId { get; set; }

    [ForeignKey(nameof(SupplierClaimId))]
    public SupplierClaim SupplierClaim { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierCredit
{
    [Key]
    public int Id { get; set; }

    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal RemainingAmount { get; set; }

    public int? SupplierClaimId { get; set; }

    [ForeignKey(nameof(SupplierClaimId))]
    public SupplierClaim? SupplierClaim { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierCreditApplication
{
    [Key]
    public int Id { get; set; }

    public int PurchaseId { get; set; }

    [ForeignKey(nameof(PurchaseId))]
    public Purchase Purchase { get; set; } = null!;

    public int SupplierCreditId { get; set; }

    [ForeignKey(nameof(SupplierCreditId))]
    public SupplierCredit SupplierCredit { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal AppliedAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TransformationYieldEvent
{
    [Key]
    public int Id { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [Required]
    public int SourceProductId { get; set; }

    [ForeignKey(nameof(SourceProductId))]
    public Product SourceProduct { get; set; } = null!;

    [Required]
    public int TargetProductId { get; set; }

    [ForeignKey(nameof(TargetProductId))]
    public Product TargetProduct { get; set; } = null!;

    [Column(TypeName = "decimal(12,3)")]
    public decimal SourceQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal TargetQty { get; set; }

    [Column(TypeName = "decimal(12,6)")]
    public decimal YieldFactorObserved { get; set; }

    [Column(TypeName = "decimal(12,6)")]
    public decimal? SuggestedYieldFactor { get; set; }

    public bool UsedSuggestedFactor { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal? DeviationPct { get; set; }

    [MaxLength(15)]
    public string? SuggestionConfidence { get; set; }

    public int? SuggestionSampleCount { get; set; }

    [MaxLength(20)]
    public string? SuggestionSource { get; set; }

    public int? OperatorSessionId { get; set; }
    public int? UserId { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

public class TransformationYieldProfile
{
    [Key]
    public int Id { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [Required]
    public int SourceProductId { get; set; }

    [ForeignKey(nameof(SourceProductId))]
    public Product SourceProduct { get; set; } = null!;

    [Required]
    public int TargetProductId { get; set; }

    [ForeignKey(nameof(TargetProductId))]
    public Product TargetProduct { get; set; } = null!;

    [Column(TypeName = "decimal(12,6)")]
    public decimal SuggestedYieldFactor { get; set; }

    [MaxLength(15)]
    public string Confidence { get; set; } = "Baja";

    public int SampleCount { get; set; }

    [Column(TypeName = "decimal(12,6)")]
    public decimal Volatility { get; set; }

    public DateTime LastRecalculatedAt { get; set; } = DateTime.UtcNow;
}

public class TransformationYieldRecalibrationLog
{
    [Key]
    public int Id { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    public int? SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public Supplier? Supplier { get; set; }

    [Required]
    public int SourceProductId { get; set; }

    [ForeignKey(nameof(SourceProductId))]
    public Product SourceProduct { get; set; } = null!;

    [Required]
    public int TargetProductId { get; set; }

    [ForeignKey(nameof(TargetProductId))]
    public Product TargetProduct { get; set; } = null!;

    [Column(TypeName = "decimal(12,6)")]
    public decimal CurrentYieldFactor { get; set; }

    [Column(TypeName = "decimal(12,6)")]
    public decimal ProposedYieldFactor { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal DeviationPct { get; set; }

    public int SampleCount { get; set; }

    [Column(TypeName = "decimal(12,6)")]
    public decimal Volatility { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    [MaxLength(500)]
    public string? DecisionNotes { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}

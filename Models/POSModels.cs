using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum CartStatus
{
    Open,
    SentToCashier,
    Converted
}

public enum MeasureUnit
{
    Unit,
    Weight
}

public enum SaleStatus
{
    Pending,
    Completed,
    Cancelled,
    Refunded,
    PendingTransfer,
    Paid,
    Voided
}

public enum PaymentStatus
{
    Pending,
    Confirmed,
    Rejected
}

public enum PaymentMethod
{
    Cash,
    Card,
    Transfer,
    Credit,
    AccountCredit,
    QrMp
}

public enum RefundPreference
{
    Cash,
    AccountCredit
}

public enum ReturnCondition
{
    Resellable,
    Waste
}

public enum CashSessionMovementType
{
    Refund,
    Expense,
    Withdrawal,
    Deposit,
    Correction
}

public enum Shift
{
    Morning,
    Afternoon,
    Night
}

public enum CashSessionStatus
{
    Open,
    Closed
}

public class Cart
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DeviceId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;

    public int? OperatorSessionId { get; set; }

    [ForeignKey(nameof(OperatorSessionId))]
    public OperatorSession? OperatorSession { get; set; }

    public int? TargetCashRegisterDeviceId { get; set; }

    [ForeignKey(nameof(TargetCashRegisterDeviceId))]
    public Device? TargetCashRegisterDevice { get; set; }

    [Required]
    public string Status { get; set; } = CartStatus.Open.ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SentToCashierAt { get; set; }

    public DateTime? ConvertedAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CartId { get; set; }

    [ForeignKey(nameof(CartId))]
    public Cart Cart { get; set; } = null!;

    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Required]
    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    public decimal UnitPrice { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = MeasureUnit.Unit.ToString();

    public decimal Discount { get; set; } = 0;

    [Column(TypeName = "decimal(12,3)")]
    public decimal ContainerReturnedNowQty { get; set; } = 0;

    public decimal Subtotal => (UnitPrice * Quantity) - Discount;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Sale
{
    [Key]
    public int Id { get; set; }

    public int? CartId { get; set; }

    [ForeignKey(nameof(CartId))]
    public Cart? Cart { get; set; }

    [Required]
    public int DeviceId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;

    public int? OperatorSessionId { get; set; }

    [ForeignKey(nameof(OperatorSessionId))]
    public OperatorSession? OperatorSession { get; set; }

    public int? CashSessionId { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession? CashSession { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [MaxLength(20)]
    public string? ShiftBucket { get; set; }

    [MaxLength(20)]
    public string? ExpectedShiftBucket { get; set; }

    [Required]
    [MaxLength(20)]
    public string ShiftAssignmentStatus { get; set; } = "Assigned";

    public DateTime? ShiftAssignedAt { get; set; }

    public int? ShiftAssignedByUsuarioId { get; set; }

    [MaxLength(250)]
    public string? ShiftAssignmentReason { get; set; }

    public bool LateShiftOpen { get; set; } = false;

    [Required]
    public string Status { get; set; } = SaleStatus.Pending.ToString();

    [Required]
    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; } = 0;

    public decimal PromoDiscount { get; set; } = 0;

    public decimal ManualDiscount { get; set; } = 0;

    public decimal CigaretteSurcharge { get; set; } = 0;

    public decimal Tax { get; set; } = 0;

    public decimal Total { get; set; }

    [MaxLength(50)]
    public string? InvoiceNumber { get; set; }

    [MaxLength(100)]
    public string? ExternalTicketId { get; set; }

    public bool CreatedOffline { get; set; } = false;

    [MaxLength(50)]
    public string? OfflineSource { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();

    public ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();
}

public class SaleItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SaleId { get; set; }

    [ForeignKey(nameof(SaleId))]
    public Sale Sale { get; set; } = null!;

    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Required]
    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    public decimal UnitPrice { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = MeasureUnit.Unit.ToString();

    public decimal Discount { get; set; } = 0;

    public decimal PromoDiscount { get; set; } = 0;

    public decimal ManualDiscount { get; set; } = 0;

    public decimal Subtotal => (UnitPrice * Quantity) - Discount - PromoDiscount - ManualDiscount;

    public decimal CigaretteSurcharge { get; set; } = 0;

    public bool HasManualPrice { get; set; } = false;

    public int? PromotionId { get; set; }

    public string? PromotionType { get; set; }

    public int? ContainerTypeId { get; set; }

    [ForeignKey(nameof(ContainerTypeId))]
    public ContainerType? ContainerType { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal ContainerOwedQty { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    public decimal ContainerDepositAmountSnapshot { get; set; } = 0;

    public ICollection<SaleReturnLine> ReturnLines { get; set; } = new List<SaleReturnLine>();
}

public class SalePayment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SaleId { get; set; }

    [ForeignKey(nameof(SaleId))]
    public Sale Sale { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = PaymentStatus.Confirmed.ToString();

    [Required]
    public decimal Amount { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    [MaxLength(40)]
    public string? Provider { get; set; }

    [MaxLength(120)]
    public string? ExternalReference { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public string? ConfirmNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SaleReturn
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OriginalSaleId { get; set; }

    [ForeignKey(nameof(OriginalSaleId))]
    public Sale OriginalSale { get; set; } = null!;

    [Required]
    public int CashSessionId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession CashSession { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string RefundPreference { get; set; } = global::KodvianSuperMarket.Models.RefundPreference.Cash.ToString();

    [Column(TypeName = "decimal(12,2)")]
    public decimal RefundTotal { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal ReturnedSubtotal { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal ReturnedCigaretteSurchargeShare { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [MaxLength(200)]
    public string? CustomerAlias { get; set; }

    [Required]
    public int CreatedByOperatorSessionId { get; set; }

    [ForeignKey(nameof(CreatedByOperatorSessionId))]
    public OperatorSession CreatedByOperatorSession { get; set; } = null!;

    [Required]
    public int CreatedByUsuarioId { get; set; }

    [ForeignKey(nameof(CreatedByUsuarioId))]
    public Usuario CreatedByUsuario { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SaleReturnLine> Lines { get; set; } = new List<SaleReturnLine>();
}

public class SaleReturnLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SaleReturnId { get; set; }

    [ForeignKey(nameof(SaleReturnId))]
    public SaleReturn SaleReturn { get; set; } = null!;

    [Required]
    public int OriginalSaleItemId { get; set; }

    [ForeignKey(nameof(OriginalSaleItemId))]
    public SaleItem OriginalSaleItem { get; set; } = null!;

    public int? ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal QtyReturned { get; set; }

    [Required]
    [MaxLength(20)]
    public string Condition { get; set; } = ReturnCondition.Resellable.ToString();

    [Column(TypeName = "decimal(12,2)")]
    public decimal LineRefundAmount { get; set; }

    public bool IsCigarette { get; set; }
}

public class CashSessionMoneyMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CashSessionId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession CashSession { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Method { get; set; } = PaymentMethod.Cash.ToString();

    [Column(TypeName = "decimal(12,2)")]
    public decimal SignedAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = CashSessionMovementType.Refund.ToString();

    [Required]
    [MaxLength(300)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(30)]
    public string? RefType { get; set; }

    public int? RefId { get; set; }

    public int? CreatedByOperatorSessionId { get; set; }

    [ForeignKey(nameof(CreatedByOperatorSessionId))]
    public OperatorSession? CreatedByOperatorSession { get; set; }

    public int? CreatedByUsuarioId { get; set; }

    [ForeignKey(nameof(CreatedByUsuarioId))]
    public Usuario? CreatedByUsuario { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CashSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DeviceId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;

    public int? OperatorSessionId { get; set; }

    [ForeignKey(nameof(OperatorSessionId))]
    public OperatorSession? OperatorSession { get; set; }

    public int? OpenedByOperatorSessionId { get; set; }

    [ForeignKey(nameof(OpenedByOperatorSessionId))]
    public OperatorSession? OpenedByOperatorSession { get; set; }

    public int? OpenedByUsuarioId { get; set; }

    [ForeignKey(nameof(OpenedByUsuarioId))]
    public Usuario? OpenedByUsuario { get; set; }

    public int? CurrentOperatorSessionId { get; set; }

    [ForeignKey(nameof(CurrentOperatorSessionId))]
    public OperatorSession? CurrentOperatorSession { get; set; }

    public int? CurrentUsuarioId { get; set; }

    [ForeignKey(nameof(CurrentUsuarioId))]
    public Usuario? CurrentUsuario { get; set; }

    public int? ClosedByOperatorSessionId { get; set; }

    [ForeignKey(nameof(ClosedByOperatorSessionId))]
    public OperatorSession? ClosedByOperatorSession { get; set; }

    public int? ClosedByUsuarioId { get; set; }

    [ForeignKey(nameof(ClosedByUsuarioId))]
    public Usuario? ClosedByUsuario { get; set; }

    [Required]
    [MaxLength(20)]
    public string Shift { get; set; } = "Morning";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = CashSessionStatus.Open.ToString();

    public decimal OpeningCash { get; set; }

    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalTransfer { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalSales => TotalCash + TotalCard + TotalTransfer + TotalCredit;

    public decimal DeclaredCash { get; set; }
    public decimal DeclaredCard { get; set; }
    public decimal DeclaredTransfer { get; set; }
    public decimal DeclaredCredit { get; set; }
    public decimal DeclaredTotal => DeclaredCash + DeclaredCard + DeclaredTransfer + DeclaredCredit;

    public decimal DiffCash => DeclaredCash - TotalCash;
    public decimal DiffCard => DeclaredCard - TotalCard;
    public decimal DiffTransfer => DeclaredTransfer - TotalTransfer;
    public decimal DiffCredit => DeclaredCredit - TotalCredit;
    public decimal DiffTotal => DeclaredTotal - TotalSales;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public string? CloseNotes { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public ICollection<SaleReturn> SaleReturns { get; set; } = new List<SaleReturn>();

    public ICollection<CashSessionMoneyMovement> MoneyMovements { get; set; } = new List<CashSessionMoneyMovement>();

    public CigaretteCount? CigaretteCount { get; set; }

    public ICollection<CashSessionHandover> Handovers { get; set; } = new List<CashSessionHandover>();
}

public class CashSessionHandover
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CashSessionId { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession CashSession { get; set; } = null!;

    public int? FromOperatorSessionId { get; set; }

    [ForeignKey(nameof(FromOperatorSessionId))]
    public OperatorSession? FromOperatorSession { get; set; }

    public int? FromUsuarioId { get; set; }

    [ForeignKey(nameof(FromUsuarioId))]
    public Usuario? FromUsuario { get; set; }

    [Required]
    public int ToOperatorSessionId { get; set; }

    [ForeignKey(nameof(ToOperatorSessionId))]
    public OperatorSession ToOperatorSession { get; set; } = null!;

    [Required]
    public int ToUsuarioId { get; set; }

    [ForeignKey(nameof(ToUsuarioId))]
    public Usuario ToUsuario { get; set; } = null!;

    [Required]
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CigaretteCount
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CashSessionId { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession CashSession { get; set; } = null!;

    public DateTime CountDate { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool AdjustmentsApplied { get; set; } = false;

    public DateTime? AdjustmentsAppliedAt { get; set; }

    public ICollection<CigaretteCountLine> Lines { get; set; } = new List<CigaretteCountLine>();
}

public class CigaretteCountLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CigaretteCountId { get; set; }

    [ForeignKey(nameof(CigaretteCountId))]
    public CigaretteCount CigaretteCount { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(12,3)")]
    public decimal SystemQtyAtCount { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,3)")]
    public decimal CountedQty { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,3)")]
    public decimal DiffQty => CountedQty - SystemQtyAtCount;

    public bool AdjustmentApplied { get; set; } = false;
}

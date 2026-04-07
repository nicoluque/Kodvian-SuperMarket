using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum PurchaseSuggestionStatus
{
    Draft,
    Converted
}

public enum PurchaseSuggestionLineStatus
{
    Pending,
    Accepted,
    Ignored,
    Converted
}

public class PurchaseSuggestion
{
    [Key]
    public int Id { get; set; }

    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    public int? GeneratedByUsuarioId { get; set; }

    [ForeignKey(nameof(GeneratedByUsuarioId))]
    public Usuario? GeneratedByUsuario { get; set; }

    public int DaysWindow { get; set; } = 14;

    [Column(TypeName = "decimal(12,3)")]
    public decimal TargetCoverageDays { get; set; } = 7;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = PurchaseSuggestionStatus.Draft.ToString();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseSuggestionLine> Lines { get; set; } = new List<PurchaseSuggestionLine>();
}

public class PurchaseSuggestionLine
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PurchaseSuggestionId { get; set; }

    [ForeignKey(nameof(PurchaseSuggestionId))]
    public PurchaseSuggestion PurchaseSuggestion { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public int? SuggestedSupplierId { get; set; }

    [ForeignKey(nameof(SuggestedSupplierId))]
    public Supplier? SuggestedSupplier { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal MinStock { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal AvgDailySales { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal TargetCoverageStock { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal SuggestedQty { get; set; }

    [Column(TypeName = "decimal(12,3)")]
    public decimal AcceptedQty { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = PurchaseSuggestionLineStatus.Pending.ToString();

    public int? CreatedPurchaseId { get; set; }

    [ForeignKey(nameof(CreatedPurchaseId))]
    public Purchase? CreatedPurchase { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

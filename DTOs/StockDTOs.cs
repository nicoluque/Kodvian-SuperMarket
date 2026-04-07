using System.ComponentModel.DataAnnotations;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.DTOs;

public class ProductStockDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StockMovementDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public decimal DeltaQty { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int? PurchaseId { get; set; }
    public int? SaleId { get; set; }
    public int? SupplierClaimId { get; set; }
    public int? OperatorSessionId { get; set; }
    public int? DeviceId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateStockMovementDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public string Bucket { get; set; } = string.Empty;

    [Required]
    public decimal DeltaQty { get; set; }

    [Required]
    public string MovementType { get; set; } = string.Empty;

    public int? PurchaseId { get; set; }
    public int? SaleId { get; set; }
    public int? SupplierClaimId { get; set; }
    public int? OperatorSessionId { get; set; }
    public int? DeviceId { get; set; }
    public string? Notes { get; set; }
}

public class SupplierClaimDto
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int? PurchaseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasReceipt { get; set; }
    public string? ReceiptType { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public string RequestedSettlementMode { get; set; } = SupplierClaimSettlementMode.Credit.ToString();
    public string? ResolvedSettlementMode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? CreditedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedByUserId { get; set; }
    public List<SupplierClaimItemDto> Items { get; set; } = new();
    public List<SupplierClaimEvidenceDto> Evidences { get; set; } = new();
    public List<SupplierClaimExchangeLineDto> ExchangeLines { get; set; } = new();
    public List<SupplierClaimRefundDto> Refunds { get; set; } = new();
}

public class SupplierClaimExchangeLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public string? Notes { get; set; }
}

public class SupplierClaimRefundDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupplierClaimEvidenceDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PreviewUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}

public class SupplierClaimItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public string? Notes { get; set; }
}

public class CreateSupplierClaimDto
{
    public int? SupplierId { get; set; }
    public int? PurchaseId { get; set; }
    public bool HasReceipt { get; set; }
    public string? ReceiptType { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public string? SettlementMode { get; set; }
    public List<CreateSupplierClaimItemDto> Items { get; set; } = new();
}

public class CreateSupplierClaimItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    public decimal UnitCostSnapshot { get; set; }
    public string? Notes { get; set; }
}

public class ResolveSupplierClaimRefundDto
{
    [Required]
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class ResolveSupplierClaimExchangeDto
{
    public string? Notes { get; set; }
    public List<ResolveSupplierClaimExchangeLineDto> Lines { get; set; } = new();
}

public class ResolveSupplierClaimExchangeLineDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    public decimal UnitCostSnapshot { get; set; }
    public string? Notes { get; set; }
}

public class SupplierCreditDto
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int? SupplierClaimId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SupplierCreditApplicationDto> Applications { get; set; } = new();
}

public class SupplierCreditApplicationDto
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public string? PurchaseNumber { get; set; }
    public decimal AppliedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupplierCreditApplicationDto
{
    [Required]
    public int SupplierCreditId { get; set; }

    [Required]
    public int PurchaseId { get; set; }

    [Required]
    public decimal AppliedAmount { get; set; }
}

public class StockBalanceDto
{
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Vendible { get; set; }
    public decimal Reclamo { get; set; }
    public decimal Merma { get; set; }
    public decimal Total => Vendible + Reclamo + Merma;
}

public class StockReportDto
{
    public List<StockBalanceDto> Balances { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class StockTransformationTemplateDto
{
    public int? SupplierId { get; set; }
    public int SourceProductId { get; set; }
    public int TargetProductId { get; set; }
    public decimal YieldFactor { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpsertStockTransformationTemplateDto
{
    public int? SupplierId { get; set; }

    [Required]
    public int SourceProductId { get; set; }

    [Required]
    public int TargetProductId { get; set; }

    [Required]
    public decimal YieldFactor { get; set; }

    public string? Notes { get; set; }
}

public class ApplyStockTransformationDto
{
    public int? SupplierId { get; set; }

    [Required]
    public int SourceProductId { get; set; }

    [Required]
    public int TargetProductId { get; set; }

    [Required]
    public decimal SourceQty { get; set; }

    public decimal? YieldFactor { get; set; }
    public decimal? TargetQty { get; set; }
    public decimal? SuggestedYieldFactor { get; set; }
    public bool? UsedSuggestedFactor { get; set; }
    public string? SuggestionConfidence { get; set; }
    public int? SuggestionSampleCount { get; set; }
    public string? SuggestionSource { get; set; }
    public string? Notes { get; set; }
}

public class StockTransformationYieldSuggestionDto
{
    public int? SupplierId { get; set; }
    public int SourceProductId { get; set; }
    public int TargetProductId { get; set; }
    public decimal SuggestedYieldFactor { get; set; }
    public decimal? ExpectedTargetQty { get; set; }
    public string Confidence { get; set; } = "Baja";
    public int SampleCount { get; set; }
    public decimal Volatility { get; set; }
    public string Source { get; set; } = "Default";
    public DateTime CalculatedAt { get; set; }
}

public class StockTransformationYieldPolicyDto
{
    public bool AutoUpdateEnabled { get; set; } = false;
    public bool RequireAdminApproval { get; set; } = true;
    public int MinSampleCount { get; set; } = 12;
    public decimal MaxVolatility { get; set; } = 0.12m;
    public decimal MaxDeviationPct { get; set; } = 15m;
    public decimal MinDeviationPct { get; set; } = 3m;
}

public class StockTransformationYieldRecalibrationDto
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public int SourceProductId { get; set; }
    public int TargetProductId { get; set; }
    public decimal CurrentYieldFactor { get; set; }
    public decimal ProposedYieldFactor { get; set; }
    public decimal DeviationPct { get; set; }
    public int SampleCount { get; set; }
    public decimal Volatility { get; set; }
    public string Status { get; set; } = "Pending";
    public string? DecisionNotes { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class DecideStockTransformationYieldRecalibrationDto
{
    public string? Notes { get; set; }
}

public class CigaretteCountDto
{
    public int Id { get; set; }
    public int CashSessionId { get; set; }
    public DateTime CountDate { get; set; }
    public string? Notes { get; set; }
    public bool AdjustmentsApplied { get; set; }
    public DateTime? AdjustmentsAppliedAt { get; set; }
    public List<CigaretteCountLineDto> Lines { get; set; } = new();
}

public class CigaretteCountLineDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal SystemQtyAtCount { get; set; }
    public decimal CountedQty { get; set; }
    public decimal DiffQty { get; set; }
    public bool AdjustmentApplied { get; set; }
}

public class CreateCigaretteCountDto
{
    public string? Notes { get; set; }
    public List<CreateCigaretteCountLineDto> Lines { get; set; } = new();
}

public class CreateCigaretteCountLineDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public decimal CountedQty { get; set; }
}

public class StockOpeningPreviewResponse
{
    public int SessionId { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ErrorRows { get; set; }
    public bool RequiresExplicitConfirmation { get; set; }
    public string? WarningMessage { get; set; }
    public List<StockOpeningPreviewLineDto> Lines { get; set; } = new();
}

public class StockOpeningPreviewLineDto
{
    public int Id { get; set; }
    public int RowNumber { get; set; }
    public int? ProductId { get; set; }
    public string? Barcode { get; set; }
    public string? QuickCode { get; set; }
    public string? ProductName { get; set; }
    public decimal CurrentVendibleQty { get; set; }
    public decimal CurrentReclamoQty { get; set; }
    public decimal CurrentMermaQty { get; set; }
    public decimal TargetVendibleQty { get; set; }
    public decimal TargetReclamoQty { get; set; }
    public decimal TargetMermaQty { get; set; }
    public decimal DeltaVendibleQty { get; set; }
    public decimal DeltaReclamoQty { get; set; }
    public decimal DeltaMermaQty { get; set; }
    public string? Error { get; set; }
}

public class StockOpeningCommitRequest
{
    [Required]
    public int SessionId { get; set; }
    public bool ExplicitConfirmation { get; set; } = false;
}

public class StockCountSessionSummaryDto
{
    public int Id { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public int ErrorLines { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CommittedAt { get; set; }
}

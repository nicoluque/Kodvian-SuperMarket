using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class SupplierCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? CUIT { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ClaimSettlementModeDefault { get; set; }
    public bool AllowClaimSettlementOverride { get; set; } = false;
}

public class SupplierUpdateRequest
{
    public string? Name { get; set; }
    public string? CUIT { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool? IsActive { get; set; }
    public string? ClaimSettlementModeDefault { get; set; }
    public bool? AllowClaimSettlementOverride { get; set; }
}

public class SupplierResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CUIT { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public string ClaimSettlementModeDefault { get; set; } = string.Empty;
    public bool AllowClaimSettlementOverride { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PurchaseCreateRequest
{
    public int? SupplierId { get; set; }
    public string DocType { get; set; } = "Invoice";
    public string? DocNumber { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public List<PurchaseItemRequest> Items { get; set; } = new();
}

public class PurchaseUpdateRequest
{
    public int? SupplierId { get; set; }
    public string? DocType { get; set; }
    public string? DocNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public List<PurchaseItemRequest>? Items { get; set; }
}

public class PurchaseItemRequest
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    public decimal Quantity { get; set; }
    [Required]
    public decimal UnitCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal DamagedForClaimQty { get; set; } = 0;
    public decimal DiscardQty { get; set; } = 0;
    public bool UpdateSalePrice { get; set; } = false;
    public decimal? NewSalePrice { get; set; }
    public decimal? NewPricePerKg { get; set; }
}

public class PurchaseResponse
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string DocType { get; set; } = string.Empty;
    public string? DocNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<PurchaseItemResponse> Items { get; set; } = new();
}

public class PurchaseItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal DamagedForClaimQty { get; set; }
    public decimal DiscardQty { get; set; }
    public decimal VendibleQty { get; set; }
    public bool UpdateSalePrice { get; set; }
    public decimal? NewSalePrice { get; set; }
    public decimal? NewPricePerKg { get; set; }
}

public class PurchaseConfirmRequest
{
}

public class PurchaseCancelRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class SupplierReturnCreateRequest
{
    public DateTime? Date { get; set; }
    public List<SupplierReturnLineRequest> Lines { get; set; } = new();
}

public class SupplierReturnLineRequest
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    public decimal Qty { get; set; }
}

public class SupplierReturnResponse
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DateTime ReturnDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SupplierReturnLineResponse> Lines { get; set; } = new();
}

public class SupplierReturnLineResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitCostSnapshot { get; set; }
}

public class ExternalExchangeCreateRequest
{
    public DateTime? Date { get; set; }
    public List<ExternalExchangeLineRequest> Lines { get; set; } = new();
}

public class ExternalExchangeLineRequest
{
    [Required]
    public string Direction { get; set; } = string.Empty;
    [Required]
    public int ProductId { get; set; }
    [Required]
    public decimal Qty { get; set; }
}

public class ExternalExchangeResponse
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public DateTime ExchangeDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ExternalExchangeLineResponse> Lines { get; set; } = new();
}

public class ExternalExchangeLineResponse
{
    public int Id { get; set; }
    public string Direction { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public decimal Qty { get; set; }
}

public class PurchaseSuggestionGenerateRequest
{
    public int? SupplierId { get; set; }
    public bool CriticalOnly { get; set; } = false;
    public int DaysWindow { get; set; } = 14;
    public decimal TargetCoverageDays { get; set; } = 7;
}

public class PurchaseSuggestionLineUpdateRequest
{
    public string? Status { get; set; }
    public decimal? AcceptedQty { get; set; }
    public string? Notes { get; set; }
}

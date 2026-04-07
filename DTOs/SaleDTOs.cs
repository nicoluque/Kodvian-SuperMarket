using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class SaleCreateRequest
{
    public int? CartId { get; set; }
    public int? CustomerId { get; set; }
    public decimal Discount { get; set; } = 0;
    public List<SalePaymentRequest> Payments { get; set; } = new();
    public int? AccountCreditCustomerId { get; set; }
}

public class SaleResponse
{
    public int Id { get; set; }
    public int? CartId { get; set; }
    public int? CustomerId { get; set; }
    public int DeviceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ShiftBucket { get; set; }
    public string? ExpectedShiftBucket { get; set; }
    public string? ShiftAssignmentStatus { get; set; }
    public DateTime? ShiftAssignedAt { get; set; }
    public bool LateShiftOpen { get; set; }
    public List<SaleItemResponse> Items { get; set; } = new();
    public List<SalePaymentResponse> Payments { get; set; } = new();
}

public class TotemTransitionReassignRequest
{
    [Required]
    public string ShiftBucket { get; set; } = string.Empty;
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class SaleItemResponse
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }
}

public class SalePaymentRequest
{
    [Required]
    public string PaymentMethod { get; set; } = string.Empty;
    [Required]
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public bool IsPending { get; set; } = false;
}

public class SalePaymentResponse
{
    public int Id { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Provider { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConfirmTransferRequest
{
    public int PaymentId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class PendingTransferResponse
{
    public int SaleId { get; set; }
    public int? CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PendingTransferPaymentResponse> Payments { get; set; } = new();
}

public class PendingTransferPaymentResponse
{
    public int Id { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}

public class ReturnEligibleSaleResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateSaleReturnRequest
{
    [Required]
    public string RefundPreference { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerAlias { get; set; }
    public List<CreateSaleReturnLineRequest> Lines { get; set; } = new();
}

public class CreateSaleReturnLineRequest
{
    [Required]
    public int OriginalSaleItemId { get; set; }
    [Required]
    public decimal QtyReturned { get; set; }
    [Required]
    public string Condition { get; set; } = string.Empty;
}

public class SaleReturnResponse
{
    public int Id { get; set; }
    public int OriginalSaleId { get; set; }
    public int CashSessionId { get; set; }
    public string RefundPreference { get; set; } = string.Empty;
    public decimal RefundTotal { get; set; }
    public decimal ReturnedSubtotal { get; set; }
    public decimal ReturnedCigaretteSurchargeShare { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerAlias { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SaleReturnLineResponse> Lines { get; set; } = new();
}

public class SaleReturnLineResponse
{
    public int Id { get; set; }
    public int OriginalSaleItemId { get; set; }
    public int? ProductId { get; set; }
    public decimal QtyReturned { get; set; }
    public string Condition { get; set; } = string.Empty;
    public decimal LineRefundAmount { get; set; }
    public bool IsCigarette { get; set; }
}

public class CancelPendingTransferRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class CancelPendingTransferWithAuthorizationRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public string ApproverUsername { get; set; } = string.Empty;

    [Required]
    public string ApproverPassword { get; set; } = string.Empty;

    [Required]
    public string ApproverPin { get; set; } = string.Empty;
}

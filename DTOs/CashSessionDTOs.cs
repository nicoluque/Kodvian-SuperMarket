using System.ComponentModel.DataAnnotations;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.DTOs;

public class CashSessionOpenRequest
{
    [Required]
    public string Shift { get; set; } = "Morning";
    [Required]
    public decimal OpeningCash { get; set; }
}

public class CashSessionCloseRequest
{
    public decimal DeclaredCash { get; set; }
    public decimal DeclaredCard { get; set; }
    public decimal DeclaredTransfer { get; set; }
    public decimal DeclaredCredit { get; set; }
    public string? Notes { get; set; }
}

public class CashSessionHandoverRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CashSessionHandoverAuthRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }

    [Required]
    public string NewOperatorUsername { get; set; } = string.Empty;

    [Required]
    public string NewOperatorPassword { get; set; } = string.Empty;

    [Required]
    public string NewOperatorPin { get; set; } = string.Empty;
}

public class CashSessionResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal OpeningCash { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalTransfer { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalSales { get; set; }
    public decimal DeclaredCash { get; set; }
    public decimal DeclaredCard { get; set; }
    public decimal DeclaredTransfer { get; set; }
    public decimal DeclaredCredit { get; set; }
    public decimal DeclaredTotal { get; set; }
    public decimal DiffCash { get; set; }
    public decimal DiffCard { get; set; }
    public decimal DiffTransfer { get; set; }
    public decimal DiffCredit { get; set; }
    public decimal DiffTotal { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CloseNotes { get; set; }
    public int SalesCount { get; set; }
    public int? OpenedByUsuarioId { get; set; }
    public string? OpenedByUsername { get; set; }
    public int? CurrentUsuarioId { get; set; }
    public string? CurrentUsername { get; set; }
    public int? ClosedByUsuarioId { get; set; }
    public string? ClosedByUsername { get; set; }
}

public class CashSessionHandoverResponse
{
    public int Id { get; set; }
    public int CashSessionId { get; set; }
    public int? FromUsuarioId { get; set; }
    public string? FromUsername { get; set; }
    public int ToUsuarioId { get; set; }
    public string ToUsername { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CashSessionCloseResponse
{
    public CashSessionResponse Session { get; set; } = null!;
    public List<string> BlockedByTasks { get; set; } = new();
    public List<string> MissingRequiredTasks { get; set; } = new();
    public bool BlockedByCigarettesCount { get; set; } = false;
    public bool HasNonBlockingPendingTasks { get; set; } = false;
    public List<string> PendingNonBlockingTasks { get; set; } = new();
}

public class CashSessionHandoverAuthResponse
{
    public CashSessionHandoverResponse Handover { get; set; } = null!;
    public OperatorSessionResponse OperatorSession { get; set; } = null!;
}

public class CreateCashSessionMoneyMovementRequest
{
    [Required]
    public string Method { get; set; } = PaymentMethod.Cash.ToString();
    [Required]
    public decimal Amount { get; set; }
    [Required]
    public string Type { get; set; } = CashSessionMovementType.Expense.ToString();
    [Required]
    public string Reason { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? RefType { get; set; }
    public int? RefId { get; set; }
}

public class CashSessionMoneyMovementResponse
{
    public int Id { get; set; }
    public int CashSessionId { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal SignedAmount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? RefType { get; set; }
    public int? RefId { get; set; }
    public int? CreatedByOperatorSessionId { get; set; }
    public int? CreatedByUsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CashSessionMoneyMovementSummaryResponse
{
    public int CashSessionId { get; set; }
    public decimal TotalSignedAmount { get; set; }
    public decimal Cash { get; set; }
    public decimal Card { get; set; }
    public decimal Transfer { get; set; }
    public decimal Credit { get; set; }
}

public class CashSessionSaleSummaryResponse
{
    public int SaleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string CustomerName { get; set; } = "Consumidor final";
    public string PaymentMethodsLabel { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
}

public class CashSessionSalesListResponse
{
    public int CashSessionId { get; set; }
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CashSessionSaleSummaryResponse> Items { get; set; } = new();
}

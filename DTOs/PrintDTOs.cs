namespace KodvianSuperMarket.DTOs;

public class PrintBrandingDto
{
    public string DisplayName { get; set; } = "Kodvian SuperMarket";
    public string? LogoUrl { get; set; }
    public string TicketHeaderText { get; set; } = "Gracias por su compra";
    public string TicketFooterText { get; set; } = "Conserve su comprobante";
    public string ReturnPolicyText { get; set; } = "Cambios dentro de 24h con ticket";
}

public class SalePrintDto
{
    public int SaleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CustomerName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public List<SalePrintLineDto> Items { get; set; } = new();
    public List<SalePrintPaymentDto> Payments { get; set; } = new();
    public PrintBrandingDto Branding { get; set; } = new();
}

public class SalePrintLineDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class SalePrintPaymentDto
{
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}

public class CustomerPaymentPrintDto
{
    public int PaymentId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public PrintBrandingDto Branding { get; set; } = new();
}

public class ReturnPrintDto
{
    public int ReturnId { get; set; }
    public int OriginalSaleId { get; set; }
    public string RefundPreference { get; set; } = string.Empty;
    public decimal RefundTotal { get; set; }
    public decimal ReturnedSubtotal { get; set; }
    public decimal ReturnedCigaretteSurchargeShare { get; set; }
    public string? CustomerAlias { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ReturnPrintLineDto> Lines { get; set; } = new();
    public PrintBrandingDto Branding { get; set; } = new();
}

public class ReturnPrintLineDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal QtyReturned { get; set; }
    public string Condition { get; set; } = string.Empty;
    public decimal LineRefundAmount { get; set; }
}

public class CashMovementPrintDto
{
    public int MovementId { get; set; }
    public int CashSessionId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal SignedAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public PrintBrandingDto Branding { get; set; } = new();
}

public class CashSessionClosePrintDto
{
    public int CashSessionId { get; set; }
    public string Shift { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalTransfer { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal DeclaredCash { get; set; }
    public decimal DeclaredCard { get; set; }
    public decimal DeclaredTransfer { get; set; }
    public decimal DeclaredCredit { get; set; }
    public decimal DiffTotal { get; set; }
    public string? CloseNotes { get; set; }
    public List<CashMovementPrintDto> Movements { get; set; } = new();
    public PrintBrandingDto Branding { get; set; } = new();
}

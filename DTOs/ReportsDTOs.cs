namespace KodvianSuperMarket.DTOs;

public class SalesShiftReportDto
{
    public int CashSessionId { get; set; }
    public string Shift { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Tickets { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalTransfer { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal CigaretteSurcharge { get; set; }
    public decimal Discounts { get; set; }
}

public class SalesDailyReportDto
{
    public DateTime Date { get; set; }
    public List<SalesShiftReportDto> Shifts { get; set; } = new();
}

public class SalesRangeDayReportDto
{
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public int Tickets { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalTransfer { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal CigaretteSurcharge { get; set; }
    public decimal Discounts { get; set; }
}

public class PendingTransferReportDto
{
    public int SaleId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public decimal PendingAmount { get; set; }
}

public class SupplierCreditBySupplierDto
{
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal RemainingCredit { get; set; }
}

public class ContainerDebtorDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal OwedQty { get; set; }
}

public class ContainerByCustomerDto
{
    public int ContainerTypeId { get; set; }
    public string ContainerTypeName { get; set; } = string.Empty;
    public decimal OwedQty { get; set; }
}

public class CigaretteCountReportDto
{
    public int CigaretteCountId { get; set; }
    public int CashSessionId { get; set; }
    public string Shift { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public decimal TotalDiffQty { get; set; }
}

public class RrhhHoursDto
{
    public int UsuarioId { get; set; }
    public string UsuarioName { get; set; } = string.Empty;
    public decimal Hours { get; set; }
}

public class PendingTransferAlertsDto
{
    public int OlderThanHours { get; set; }
    public int TotalPendingTransfers { get; set; }
    public int AlertCount { get; set; }
    public List<int> AlertSaleIds { get; set; } = new();
}

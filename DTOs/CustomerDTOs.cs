using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class CustomerCreateRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;
    public string? DNI { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? PhoneBackup { get; set; }
    public DateTime? BirthDate { get; set; }
    public bool IsFixedCustomer { get; set; } = false;
    public bool AllowsCredit { get; set; } = false;
    public decimal CreditLimit { get; set; } = 0;
    public string? Status { get; set; }
}

public class CustomerUpdateRequest
{
    public string? FullName { get; set; }
    public string? DNI { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? PhoneBackup { get; set; }
    public DateTime? BirthDate { get; set; }
    public bool? IsFixedCustomer { get; set; }
    public bool? AllowsCredit { get; set; }
    public decimal? CreditLimit { get; set; }
    public bool? IsActive { get; set; }
    public string? Status { get; set; }
}

public class CustomerStatusUpdateRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class CustomerStatusReasonRequest
{
    public string? Reason { get; set; }
}

public class CustomerResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DNI { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? PhoneBackup { get; set; }
    public DateTime? BirthDate { get; set; }
    public bool IsFixedCustomer { get; set; }
    public bool AllowsCredit { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string EffectiveStatus { get; set; } = string.Empty;
    public decimal CurrentDebt { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal CreditUsedPct { get; set; }
    public bool IsCritical { get; set; }
    public bool IsCreditBlocked { get; set; }
}

public class AnonymousCustomerCreateRequest
{
    [Required]
    public string Alias { get; set; } = string.Empty;
}

public class CustomerContainerSummaryResponse
{
    public int CustomerId { get; set; }
    public List<CustomerContainerOwedItem> OwedByType { get; set; } = new();
}

public class CustomerContainerOwedItem
{
    public int ContainerTypeId { get; set; }
    public string ContainerTypeName { get; set; } = string.Empty;
    public decimal OwedQty { get; set; }
}

public class CustomerContainerReturnRequest
{
    [Required]
    public int ContainerTypeId { get; set; }
    [Required]
    public decimal Qty { get; set; }
}

public class CustomerAccountSummaryResponse
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalDebt { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal CreditLimit { get; set; }
    public bool HasOverdueDebt { get; set; }
}

public class CustomerAccountMovementsResponse
{
    public int CustomerId { get; set; }
    public List<MovementResponse> Movements { get; set; } = new();
}

public class MovementResponse
{
    public int Id { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAllocated { get; set; }
}

public class CustomerPaymentRequest
{
    [Required]
    public int CustomerId { get; set; }
    [Required]
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class CustomerPaymentResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public List<AllocationResponse> Allocations { get; set; } = new();
    public decimal RemainingCredit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AllocationResponse
{
    public int StatementId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
}

public class GenerateStatementRequest
{
    [Required]
    public int Year { get; set; }
    [Required]
    public int Month { get; set; }
    public int? CustomerId { get; set; }
}

public class StatementResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal Purchases { get; set; }
    public decimal Payments { get; set; }
    public decimal LateFees { get; set; }
    public decimal FinalBalance { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class RunLateFeeRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal LateFeePercentage { get; set; } = 5;
}

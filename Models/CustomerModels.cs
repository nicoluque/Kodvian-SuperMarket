using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DNI { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? PhoneBackup { get; set; }

    public DateTime? BirthDate { get; set; }

    public bool IsFixedCustomer { get; set; } = false;

    public bool AllowsCredit { get; set; } = false;

    [Column(TypeName = "decimal(12,2)")]
    public decimal CreditLimit { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public bool IsAnonymous { get; set; } = false;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = CustomerStatus.Active.ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<CustomerAccountMovement> AccountMovements { get; set; } = new List<CustomerAccountMovement>();
    public ICollection<CustomerMonthlyStatement> MonthlyStatements { get; set; } = new List<CustomerMonthlyStatement>();
    public ICollection<ContainerMovement> ContainerMovements { get; set; } = new List<ContainerMovement>();
}

public enum CustomerStatus
{
    Active,
    Inactive,
    Pending
}

public enum ContainerDirection
{
    Given,
    Returned
}

public enum MovementType
{
    CreditPurchase,
    Payment,
    LateFee,
    Adjustment,
    CreditNote,
    ReturnCredit,
    ContainerCharge,
    ContainerRefund
}

public class CustomerAccountMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string MovementType { get; set; } = "CreditPurchase";

    [Required]
    [MaxLength(30)]
    public string ReferenceType { get; set; } = string.Empty;

    public int? ReferenceId { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? AllocatedStatementId { get; set; }

    [ForeignKey(nameof(AllocatedStatementId))]
    public CustomerMonthlyStatement? AllocatedStatement { get; set; }
}

public class CustomerMonthlyStatement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [Required]
    public int Year { get; set; }

    [Required]
    public int Month { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal InitialBalance { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Purchases { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Payments { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal LateFees { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal FinalBalance { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal LateFeeAccrued { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal PaidAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal RemainingBalance { get; set; }

    public DateTime DueDate { get; set; }

    public bool IsPaid { get; set; } = false;

    public DateTime? PaidAt { get; set; }

    public DateTime? LateFeeAppliedAt { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal LateFeeAppliedAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CustomerStatementAllocation> Allocations { get; set; } = new List<CustomerStatementAllocation>();

    [MaxLength(10)]
    public string UniqueKey => $"{CustomerId}-{Year}-{Month}";
}

public class CustomerStatementAllocation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StatementId { get; set; }

    [ForeignKey(nameof(StatementId))]
    public CustomerMonthlyStatement Statement { get; set; } = null!;

    [Required]
    public int MovementId { get; set; }

    [ForeignKey(nameof(MovementId))]
    public CustomerAccountMovement Movement { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;
}

public class ContainerMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [Required]
    public int ContainerTypeId { get; set; }

    [ForeignKey(nameof(ContainerTypeId))]
    public ContainerType ContainerType { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Direction { get; set; } = ContainerDirection.Given.ToString();

    [Column(TypeName = "decimal(12,3)")]
    public decimal Qty { get; set; }

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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum TimePunchType
{
    Entry,
    Exit
}

public class TimePunch
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    public int? CashSessionId { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession? CashSession { get; set; }

    [Required]
    public int DeviceId { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public Device Device { get; set; } = null!;

    public int? OperatorSessionId { get; set; }

    [ForeignKey(nameof(OperatorSessionId))]
    public OperatorSession? OperatorSession { get; set; }

    [Required]
    public string PunchType { get; set; } = TimePunchType.Entry.ToString();

    [Required]
    public bool IsOpen { get; set; } = false;

    public DateTime PunchTime { get; set; } = DateTime.UtcNow;

    public bool IsAdjusted { get; set; } = false;

    public DateTime? AdjustedAt { get; set; }

    public int? AdjustedById { get; set; }

    [ForeignKey(nameof(AdjustedById))]
    public Usuario? AdjustedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TimePunchAdjustment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TimePunchId { get; set; }

    [ForeignKey(nameof(TimePunchId))]
    public TimePunch TimePunch { get; set; } = null!;

    [Required]
    public int AdjustedById { get; set; }

    [ForeignKey(nameof(AdjustedById))]
    public Usuario AdjustedBy { get; set; } = null!;

    [Required]
    public DateTime OriginalPunchTime { get; set; }

    [Required]
    public DateTime NewPunchTime { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EmployeeExtra
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [Required]
    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public Usuario CreatedBy { get; set; } = null!;

    [Required]
    public DateTime ExtraDate { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    public int Month { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Hours { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public bool IsApproved { get; set; } = false;

    public int? ApprovedById { get; set; }

    [ForeignKey(nameof(ApprovedById))]
    public Usuario? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PayrollReceipt
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [Required]
    public int Year { get; set; }

    [Required]
    public int Month { get; set; }

    [Required]
    [MaxLength(100)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public byte[] FileContent { get; set; } = Array.Empty<byte>();

    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = "application/pdf";

    [Required]
    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

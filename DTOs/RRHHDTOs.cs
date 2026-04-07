using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class TimePunchDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioName { get; set; }
    public int? CashSessionId { get; set; }
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string PunchType { get; set; } = string.Empty;
    public DateTime PunchTime { get; set; }
    public bool IsAdjusted { get; set; }
    public DateTime? AdjustedAt { get; set; }
    public int? AdjustedById { get; set; }
    public string? AdjustedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TimePunchResponseDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioName { get; set; }
    public string PunchType { get; set; } = string.Empty;
    public DateTime PunchTime { get; set; }
    public bool IsAdjusted { get; set; }
}

public class CreateTimePunchAdjustmentDto
{
    [Required]
    public int TimePunchId { get; set; }

    [Required]
    public DateTime NewPunchTime { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class TimePunchAdjustmentDto
{
    public int Id { get; set; }
    public int TimePunchId { get; set; }
    public int AdjustedById { get; set; }
    public string? AdjustedByName { get; set; }
    public DateTime OriginalPunchTime { get; set; }
    public DateTime NewPunchTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TimePunchInconsistencyDto
{
    public int UsuarioId { get; set; }
    public string? UsuarioName { get; set; }
    public DateTime Date { get; set; }
    public DateTime? EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public string InconsistencyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EmployeeExtraDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioName { get; set; }
    public int CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime ExtraDate { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Hours { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public int? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEmployeeExtraDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public DateTime ExtraDate { get; set; }

    [Required]
    [Range(0.01, 24)]
    public decimal Hours { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ApproveEmployeeExtraDto
{
    [Required]
    public bool Approve { get; set; }
}

public class PayrollReceiptDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioName { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UploadPayrollReceiptDto
{
    [Required]
    public int UsuarioId { get; set; }

    [Required]
    public int Year { get; set; }

    [Required]
    [Range(1, 12)]
    public int Month { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string FileContentBase64 { get; set; } = string.Empty;
}

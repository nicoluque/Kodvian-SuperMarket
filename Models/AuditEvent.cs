using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum AuditEventType
{
    Login,
    Logout,
    LoginFailed,
    SessionCreated,
    SessionRevoked,
    SessionExpired,
    PasswordChanged,
    PinChanged,
    DeviceRegistered,
    DeviceRevoked,
    RoleChanged,
    UserCreated,
    UserDeactivated,
    UserActivated,
    UnauthorizedAccess,
    MP_WEBHOOK_RECEIVED,
    MP_PAYMENT_CONFIRMED,
    MP_PAYMENT_REJECTED,
    RECEIPT_REPRINTED,
    Other
}

public class PaymentProviderEvent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string EventId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EventType { get; set; }

    [MaxLength(120)]
    public string? ExternalReference { get; set; }

    public string PayloadJson { get; set; } = "{}";

    [MaxLength(20)]
    public string Status { get; set; } = "Received";

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
}

public class AuditEvent
{
    [Key]
    public int Id { get; set; }

    public int? UsuarioId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario? Usuario { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = AuditEventType.Other.ToString();

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? DeviceType { get; set; }

    [MaxLength(2000)]
    public string? AdditionalData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool Success { get; set; } = true;
}

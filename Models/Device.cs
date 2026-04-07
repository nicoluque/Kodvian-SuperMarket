using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public class Device
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UsuarioId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [Required]
    [MaxLength(256)]
    public string TokenHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DeviceName { get; set; }

    [MaxLength(100)]
    public string? DeviceType { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastSeenAt { get; set; }

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }

    public int? ParentCashRegisterDeviceId { get; set; }

    [ForeignKey(nameof(ParentCashRegisterDeviceId))]
    public Device? ParentCashRegisterDevice { get; set; }

    public ICollection<Device> ChildDevices { get; set; } = new List<Device>();

    public ICollection<CashSession> CashSessions { get; set; } = new List<CashSession>();
}

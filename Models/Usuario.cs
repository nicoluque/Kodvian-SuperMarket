using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum UserRole
{
    Operator,
    Supervisor,
    Admin
}

public class Usuario
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? PinHash { get; set; }

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = UserRole.Operator.ToString();

    [Required]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<OperatorSession> OperatorSessions { get; set; } = new List<OperatorSession>();

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<StoreUser> StoreUsers { get; set; } = new List<StoreUser>();
}

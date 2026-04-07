using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public class Tenant
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsTrainingTenant { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Store> Stores { get; set; } = new List<Store>();
    public ICollection<Usuario> Users { get; set; } = new List<Usuario>();
    public TenantBrandingSettings? BrandingSettings { get; set; }
}

public class Store
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant Tenant { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public string SettingsJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StoreUser> StoreUsers { get; set; } = new List<StoreUser>();
}

public class StoreUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store Store { get; set; } = null!;

    [Required]
    public int UsuarioId { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = UserRole.Operator.ToString();

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

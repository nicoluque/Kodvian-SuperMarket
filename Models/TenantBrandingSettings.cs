using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public class TenantBrandingSettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant Tenant { get; set; } = null!;

    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(800)]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    public string PrimaryColor { get; set; } = "#1f7f57";

    [MaxLength(20)]
    public string SecondaryColor { get; set; } = "#27313f";

    [MaxLength(500)]
    public string TicketHeaderText { get; set; } = "Gracias por su compra";

    [MaxLength(500)]
    public string TicketFooterText { get; set; } = "Conserve su comprobante";

    [MaxLength(800)]
    public string ReturnPolicyText { get; set; } = "Cambios dentro de 24h con ticket";

    [MaxLength(60)]
    public string? SupportPhone { get; set; }

    [MaxLength(200)]
    public string? SupportEmail { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class ManualSaleImportRequest
{
    [Required]
    public string ExternalTicketId { get; set; } = string.Empty;
    [Required]
    public DateTime OriginalCreatedAt { get; set; }
    public string? CustomerAlias { get; set; }
    public List<ManualSaleImportLineRequest> Items { get; set; } = new();
}

public class ManualSaleImportLineRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;
    [Required]
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}

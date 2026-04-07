using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class CartCreateRequest
{
}

public class CartResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentToCashierAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public List<CartItemResponse> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Subtotal);
}

public class CartItemCreateRequest
{
    public int? ProductId { get; set; }
    [Required]
    public string ProductCode { get; set; } = string.Empty;
    [Required]
    public string ProductName { get; set; } = string.Empty;
    [Required]
    public decimal UnitPrice { get; set; }
    [Required]
    public decimal Quantity { get; set; }
    [Required]
    public string Unit { get; set; } = "Unit";
    public decimal Discount { get; set; } = 0;
    public decimal ContainerReturnedNowQty { get; set; } = 0;
}

public class CartItemUpdateRequest
{
    public decimal? UnitPrice { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Discount { get; set; }
    public decimal? ContainerReturnedNowQty { get; set; }
}

public class CartItemResponse
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal ContainerReturnedNowQty { get; set; }
    public decimal Subtotal { get; set; }
}

public class SendToCashierRequest
{
}

public class CartInboxResponse
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentToCashierAt { get; set; }
    public List<CartItemResponse> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Subtotal);
}

public class CartContainerCheckItemResponse
{
    public int ItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ContainerReturnedNowQty { get; set; }
    public decimal OwedQty { get; set; }
}

public class CartContainerCheckResponse
{
    public int CartId { get; set; }
    public bool HasOwedContainers { get; set; }
    public List<CartContainerCheckItemResponse> Items { get; set; } = new();
}

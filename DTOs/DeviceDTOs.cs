namespace KodvianSuperMarket.DTOs;

public class DeviceCreateRequest
{
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int? StoreId { get; set; }
}

public class DeviceResponse
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsRevoked { get; set; }
}

public class DeviceListResponse
{
    public int Id { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsRevoked { get; set; }
}

public class DeviceRotateResponse
{
    public string NewToken { get; set; } = string.Empty;
}

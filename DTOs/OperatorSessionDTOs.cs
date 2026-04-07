namespace KodvianSuperMarket.DTOs;

public class OperatorSessionCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Pin { get; set; }
}

public class OperatorSessionResponse
{
    public string SessionToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int UsuarioId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class OperatorSessionRefreshRequest
{
    public string? Pin { get; set; }
}

public class OperatorSessionRevokeRequest
{
    public string? Reason { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Pin { get; set; }
}

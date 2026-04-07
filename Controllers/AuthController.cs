using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IOperatorSessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly ApplicationDbContext _dbContext;

    public AuthController(IOperatorSessionService sessionService, IAuditService auditService, ApplicationDbContext dbContext)
    {
        _sessionService = sessionService;
        _auditService = auditService;
        _dbContext = dbContext;
    }

    [HttpPost("operator-session")]
    [DeviceAuth]
    public async Task<ActionResult<OperatorSessionResponse>> CreateSession([FromBody] OperatorSessionCreateRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var deviceType = Request.Headers["Device-Type"].FirstOrDefault();

        var usuario = await _sessionService.ValidateCredentialsAsync(request.Username, request.Password, request.Pin);

        if (usuario == null)
        {
            await _auditService.LogAsync(
                AuditEventType.LoginFailed,
                null,
                $"Failed login attempt for username: {request.Username}",
                ipAddress,
                userAgent,
                deviceType,
                $"Username: {request.Username}",
                false
            );
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        var device = HttpContext.Items.TryGetValue("Device", out var devObj) ? devObj as Device : null;
        if (!await IsUserAllowedForDeviceAsync(usuario, device))
        {
            await _auditService.LogAsync(
                AuditEventType.UnauthorizedAccess,
                usuario.Id,
                $"User {usuario.Username} is not allowed for current device/store",
                ipAddress,
                userAgent,
                deviceType,
                $"DeviceId: {device?.Id}, StoreId: {device?.StoreId}",
                false
            );
            return Forbid();
        }

        var ownershipConflict = await GetOpenCashSessionOwnershipConflictAsync(usuario, device);
        if (!string.IsNullOrWhiteSpace(ownershipConflict))
        {
            await _auditService.LogAsync(
                AuditEventType.UnauthorizedAccess,
                usuario.Id,
                ownershipConflict,
                ipAddress,
                userAgent,
                deviceType,
                $"DeviceId: {device?.Id}, StoreId: {device?.StoreId}",
                false
            );
            return BadRequest(new { message = ownershipConflict });
        }

        var (session, rawToken) = await _sessionService.CreateAsync(
            usuario.Id,
            ipAddress,
            userAgent,
            deviceType,
            "New session"
        );

        await _auditService.LogAsync(
            AuditEventType.Login,
            usuario.Id,
            $"User logged in: {usuario.Username}",
            ipAddress,
            userAgent,
            deviceType,
            $"SessionId: {session.Id}",
            true
        );

        return Ok(new OperatorSessionResponse
        {
            SessionToken = rawToken,
            ExpiresAt = session.ExpiresAt,
            UsuarioId = usuario.Id,
            Username = usuario.Username,
            Role = usuario.Role
        });
    }

    private async Task<bool> IsUserAllowedForDeviceAsync(Usuario usuario, Device? device)
    {
        if (device == null)
            return false;

        if (!device.StoreId.HasValue)
            return true;

        var storeId = device.StoreId.Value;
        var storeTenantId = await _dbContext.Stores
            .Where(s => s.Id == storeId)
            .Select(s => (int?)s.TenantId)
            .FirstOrDefaultAsync();

        if (usuario.TenantId.HasValue && storeTenantId.HasValue && usuario.TenantId.Value != storeTenantId.Value)
            return false;

        var isOperator = usuario.Role == UserRole.Operator.ToString();
        if (!isOperator)
            return true;

        return await _dbContext.StoreUsers.AnyAsync(su =>
            su.UsuarioId == usuario.Id &&
            su.StoreId == storeId &&
            su.IsActive);
    }

    private async Task<string?> GetOpenCashSessionOwnershipConflictAsync(Usuario usuario, Device? device)
    {
        if (device?.DeviceType != "CashRegister")
            return null;

        var openSession = await _dbContext.CashSessions
            .Where(cs => cs.DeviceId == device.Id && cs.Status == CashSessionStatus.Open.ToString())
            .Select(cs => new { cs.OperatorSessionId })
            .FirstOrDefaultAsync();

        if (openSession?.OperatorSessionId == null)
            return null;

        var owner = await _dbContext.OperatorSessions
            .Where(os => os.Id == openSession.OperatorSessionId.Value)
            .Join(_dbContext.Usuarios, os => os.UsuarioId, u => u.Id, (os, u) => new { u.Id, u.Username })
            .FirstOrDefaultAsync();

        if (owner == null || owner.Id == usuario.Id)
            return null;

        return $"La caja ya tiene una sesion activa iniciada por {owner.Username}. Debe cerrarla el mismo operador o intervenir un supervisor.";
    }

    [HttpPost("operator-session/refresh")]
    [OperatorSessionAuth]
    public async Task<ActionResult<OperatorSessionResponse>> RefreshSession([FromBody] OperatorSessionRefreshRequest? request)
    {
        var sessionId = (int)HttpContext.Items["SessionId"]!;
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        var providedPin = request?.Pin?.Trim();
        if (!string.IsNullOrWhiteSpace(providedPin))
        {
            var isPinValid = await _sessionService.ValidatePinAsync(sessionUsuarioId, providedPin);
            if (!isPinValid)
                return Unauthorized(new { message = "PIN del operador inválido" });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var deviceType = Request.Headers["Device-Type"].FirstOrDefault();

        var (session, newToken) = await _sessionService.RefreshAsync(sessionId, sessionUsuarioId);

        var usuario = await _dbContext.Usuarios.FindAsync(sessionUsuarioId);

        if (usuario == null)
            return Unauthorized();

        await _auditService.LogAsync(
            AuditEventType.SessionCreated,
            sessionUsuarioId,
            $"Session refreshed: {usuario.Username}",
            ipAddress,
            userAgent,
            deviceType,
            $"SessionId: {sessionId}",
            true
        );

        return Ok(new OperatorSessionResponse
        {
            SessionToken = newToken,
            ExpiresAt = session.ExpiresAt,
            UsuarioId = usuario.Id,
            Username = usuario.Username,
            Role = usuario.Role
        });
    }

    [HttpPost("operator-session/revoke")]
    [OperatorSessionAuth]
    public async Task<ActionResult> RevokeSession([FromBody] OperatorSessionRevokeRequest? request)
    {
        var sessionId = (int)HttpContext.Items["SessionId"]!;
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var deviceType = Request.Headers["Device-Type"].FirstOrDefault();

        var usuario = await _dbContext.Usuarios.FindAsync(sessionUsuarioId);

        await _sessionService.RevokeAsync(sessionId, sessionUsuarioId, request?.Reason);

        await _auditService.LogAsync(
            AuditEventType.SessionRevoked,
            sessionUsuarioId,
            $"Session revoked: {usuario?.Username}",
            ipAddress,
            userAgent,
            deviceType,
            $"SessionId: {sessionId}, Reason: {request?.Reason}",
            true
        );

        return Ok(new { message = "Session revoked successfully" });
    }

    [HttpPost("login")]
    public async Task<ActionResult<OperatorSessionResponse>> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var deviceType = Request.Headers["Device-Type"].FirstOrDefault();

        var usuario = await _sessionService.ValidateCredentialsAsync(request.Username, request.Password, request.Pin);

        if (usuario == null)
        {
            await _auditService.LogAsync(
                AuditEventType.LoginFailed,
                null,
                $"Failed login attempt for username: {request.Username}",
                ipAddress,
                userAgent,
                deviceType,
                $"Username: {request.Username}",
                false
            );
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        var (session, rawToken) = await _sessionService.CreateAsync(
            usuario.Id,
            ipAddress,
            userAgent,
            deviceType,
            "New session"
        );

        await _auditService.LogAsync(
            AuditEventType.Login,
            usuario.Id,
            $"User logged in: {usuario.Username}",
            ipAddress,
            userAgent,
            deviceType,
            $"SessionId: {session.Id}",
            true
        );

        return Ok(new OperatorSessionResponse
        {
            SessionToken = rawToken,
            ExpiresAt = session.ExpiresAt,
            UsuarioId = usuario.Id,
            Username = usuario.Username,
            Role = usuario.Role
        });
    }

    [HttpPost("bo-login")]
    public async Task<ActionResult<OperatorSessionResponse>> BackofficeLogin([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var deviceType = Request.Headers["Device-Type"].FirstOrDefault();

        var usuario = await _sessionService.ValidateCredentialsAsync(request.Username, request.Password, request.Pin);

        if (usuario == null)
        {
            await _auditService.LogAsync(
                AuditEventType.LoginFailed,
                null,
                $"Failed BO login attempt for username: {request.Username}",
                ipAddress,
                userAgent,
                deviceType,
                $"Username: {request.Username}",
                false
            );
            return Unauthorized(new { message = "Credenciales inválidas" });
        }

        var allowedBoRoles = new[] { UserRole.Admin.ToString(), UserRole.Supervisor.ToString(), "Manager" };
        if (!allowedBoRoles.Contains(usuario.Role))
        {
            await _auditService.LogAsync(
                AuditEventType.UnauthorizedAccess,
                usuario.Id,
                $"Rejected BO login by role: {usuario.Role}",
                ipAddress,
                userAgent,
                deviceType,
                $"Username: {usuario.Username}",
                false
            );
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Role does not have Backoffice access" });
        }

        var (session, rawToken) = await _sessionService.CreateAsync(
            usuario.Id,
            ipAddress,
            userAgent,
            deviceType,
            "Backoffice session"
        );

        await _auditService.LogAsync(
            AuditEventType.Login,
            usuario.Id,
            $"BO login success: {usuario.Username}",
            ipAddress,
            userAgent,
            deviceType,
            $"SessionId: {session.Id}",
            true
        );

        return Ok(new OperatorSessionResponse
        {
            SessionToken = rawToken,
            ExpiresAt = session.ExpiresAt,
            UsuarioId = usuario.Id,
            Username = usuario.Username,
            Role = usuario.Role
        });
    }

    [HttpPost("logout")]
    [OperatorSessionAuth]
    public async Task<ActionResult> Logout()
    {
        var sessionId = (int)HttpContext.Items["SessionId"]!;
        var sessionUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var deviceType = Request.Headers["Device-Type"].FirstOrDefault();

        var usuario = await _dbContext.Usuarios.FindAsync(sessionUsuarioId);

        await _sessionService.RevokeAsync(sessionId, sessionUsuarioId, "User logged out");

        await _auditService.LogAsync(
            AuditEventType.Logout,
            sessionUsuarioId,
            $"User logged out: {usuario?.Username}",
            ipAddress,
            userAgent,
            deviceType,
            $"SessionId: {sessionId}",
            true
        );

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("device/validate")]
    [DeviceAuth]
    public ActionResult<object> ValidateDevice()
    {
        var deviceId = HttpContext.Items.TryGetValue("DeviceId", out var did) && did is int d ? d : (int?)null;
        var storeId = HttpContext.Items.TryGetValue("StoreId", out var sid) && sid is int s ? s : (int?)null;
        var tenantId = HttpContext.Items.TryGetValue("TenantId", out var tid) && tid is int t ? t : (int?)null;
        var device = HttpContext.Items.TryGetValue("Device", out var dev) ? dev as Device : null;

        var operatingMode = "MiniMarketFull";
        object enabledModules = new
        {
            tablet = true,
            envases = true,
            cuentaCorriente = true,
            comprasSugeridas = true,
            reportes = true
        };

        if (storeId.HasValue)
        {
            var store = _dbContext.Stores.AsNoTracking().FirstOrDefault(x => x.Id == storeId.Value);
            if (store != null && !string.IsNullOrWhiteSpace(store.SettingsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(store.SettingsJson);
                    if (doc.RootElement.TryGetProperty("operatingMode", out var modeEl))
                        operatingMode = modeEl.GetString() ?? operatingMode;

                    if (doc.RootElement.TryGetProperty("enabledModules", out var modulesEl) && modulesEl.ValueKind == JsonValueKind.Object)
                    {
                        enabledModules = new
                        {
                            tablet = modulesEl.TryGetProperty("tablet", out var tabletEl) ? tabletEl.GetBoolean() : true,
                            envases = modulesEl.TryGetProperty("envases", out var envasesEl) ? envasesEl.GetBoolean() : true,
                            cuentaCorriente = modulesEl.TryGetProperty("cuentaCorriente", out var ccEl) ? ccEl.GetBoolean() : true,
                            comprasSugeridas = modulesEl.TryGetProperty("comprasSugeridas", out var csEl) ? csEl.GetBoolean() : true,
                            reportes = modulesEl.TryGetProperty("reportes", out var repEl) ? repEl.GetBoolean() : true
                        };
                    }
                }
                catch
                {
                }
            }
        }

        return Ok(new
        {
            valid = true,
            deviceId,
            storeId,
            tenantId,
            deviceType = device?.DeviceType,
            deviceName = device?.DeviceName,
            operatingMode,
            enabledModules
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/devices")]
[DeviceAuth]
[AdminOnly]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly IAuditService _auditService;

    public DevicesController(IDeviceService deviceService, IAuditService auditService)
    {
        _deviceService = deviceService;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<ActionResult<DeviceResponse>> Create([FromBody] DeviceCreateRequest request)
    {
        var deviceUsuarioId = (int)HttpContext.Items["DeviceUsuarioId"]!;
        
        var (device, rawToken) = await _deviceService.CreateAsync(
            deviceUsuarioId,
            request.DeviceName,
            request.DeviceType,
            request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.UserAgent ?? HttpContext.Request.Headers.UserAgent.ToString(),
            request.StoreId ?? (HttpContext.Items.TryGetValue("StoreId", out var sidObj) && sidObj is int sid ? sid : (int?)null)
        );

        var response = new DeviceResponse
        {
            Id = device.Id,
            Token = rawToken,
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            IpAddress = device.IpAddress,
            CreatedAt = device.CreatedAt,
            LastSeenAt = device.LastSeenAt,
            IsRevoked = device.IsRevoked
        };

        await _auditService.LogAsync(
            AuditEventType.DeviceRegistered,
            deviceUsuarioId,
            $"Device created: {device.DeviceName}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers.UserAgent.ToString(),
            device.DeviceType,
            $"DeviceId: {device.Id}",
            true
        );

        return CreatedAtAction(nameof(GetById), new { id = device.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceListResponse>>> List()
    {
        var deviceUsuarioId = (int)HttpContext.Items["DeviceUsuarioId"]!;
        var devices = await _deviceService.GetByUsuarioAsync(deviceUsuarioId);

        var response = devices.Select(d => new DeviceListResponse
        {
            Id = d.Id,
            DeviceName = d.DeviceName,
            DeviceType = d.DeviceType,
            IpAddress = d.IpAddress,
            CreatedAt = d.CreatedAt,
            LastSeenAt = d.LastSeenAt,
            IsRevoked = d.IsRevoked
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DeviceResponse>> GetById(int id)
    {
        var deviceUsuarioId = (int)HttpContext.Items["DeviceUsuarioId"]!;
        var device = await _deviceService.GetByIdAsync(id, deviceUsuarioId);

        if (device == null)
            return NotFound();

        var response = new DeviceResponse
        {
            Id = device.Id,
            Token = "***hidden***",
            DeviceName = device.DeviceName,
            DeviceType = device.DeviceType,
            IpAddress = device.IpAddress,
            CreatedAt = device.CreatedAt,
            LastSeenAt = device.LastSeenAt,
            IsRevoked = device.IsRevoked
        };

        return Ok(response);
    }

    [HttpPost("{id}/rotate-token")]
    public async Task<ActionResult<DeviceRotateResponse>> RotateToken(int id)
    {
        var deviceUsuarioId = (int)HttpContext.Items["DeviceUsuarioId"]!;
        
        var device = await _deviceService.GetByIdAsync(id, deviceUsuarioId);
        if (device == null)
            return NotFound();

        var oldTokenHash = device.TokenHash;
        var (_, newToken) = await _deviceService.RotateTokenAsync(id, deviceUsuarioId);

        await _auditService.LogAsync(
            AuditEventType.DeviceRegistered,
            deviceUsuarioId,
            $"Device token rotated: {device.DeviceName}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers.UserAgent.ToString(),
            device.DeviceType,
            $"DeviceId: {id}, OldTokenHash: {oldTokenHash[..Math.Min(16, oldTokenHash.Length)]}...",
            true
        );

        return Ok(new DeviceRotateResponse { NewToken = newToken });
    }
}

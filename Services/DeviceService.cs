using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IDeviceService
{
    Task<(Device? Device, string? RawToken)> AuthenticateAsync(string? tokenHash);
    Task<(Device Device, string RawToken)> CreateAsync(int usuarioId, string? deviceName, string? deviceType, string? ipAddress, string? userAgent, int? storeId = null);
    Task<(Device Device, string NewToken)> RotateTokenAsync(int deviceId, int usuarioId);
    Task<List<Device>> GetByUsuarioAsync(int usuarioId);
    Task<Device?> GetByIdAsync(int id, int usuarioId);
}

public class DeviceService : IDeviceService
{
    private readonly ApplicationDbContext _context;
    private readonly IHashService _hashService;

    public DeviceService(ApplicationDbContext context, IHashService hashService)
    {
        _context = context;
        _hashService = hashService;
    }

    public async Task<(Device? Device, string? RawToken)> AuthenticateAsync(string? tokenHash)
    {
        if (string.IsNullOrEmpty(tokenHash))
            return (null, null);

        var hash = _hashService.HashSha256(tokenHash);
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.TokenHash == hash && !d.IsRevoked);

        if (device == null)
            return (null, null);

        device.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (device, tokenHash);
    }

    public async Task<(Device Device, string RawToken)> CreateAsync(int usuarioId, string? deviceName, string? deviceType, string? ipAddress, string? userAgent, int? storeId = null)
    {
        var rawToken = _hashService.GenerateToken();
        var tokenHash = _hashService.HashSha256(rawToken);

        var device = new Device
        {
            UsuarioId = usuarioId,
            StoreId = storeId,
            TokenHash = tokenHash,
            DeviceName = deviceName,
            DeviceType = deviceType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        return (device, rawToken);
    }

    public async Task<(Device Device, string NewToken)> RotateTokenAsync(int deviceId, int usuarioId)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UsuarioId == usuarioId);

        if (device == null)
            throw new InvalidOperationException("Device not found");

        var rawToken = _hashService.GenerateToken();
        device.TokenHash = _hashService.HashSha256(rawToken);
        device.LastSeenAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (device, rawToken);
    }

    public async Task<List<Device>> GetByUsuarioAsync(int usuarioId)
    {
        return await _context.Devices
            .Where(d => d.UsuarioId == usuarioId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Device?> GetByIdAsync(int id, int usuarioId)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId);
    }
}

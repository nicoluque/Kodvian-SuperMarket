using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IAuditService
{
    Task LogAsync(AuditEventType eventType, int? usuarioId, string? description, string? ipAddress, string? userAgent, string? deviceType, string? additionalData, bool success = true, int? storeId = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditEventType eventType, int? usuarioId, string? description, string? ipAddress, string? userAgent, string? deviceType, string? additionalData, bool success = true, int? storeId = null)
    {
        var auditEvent = new AuditEvent
        {
            UsuarioId = usuarioId,
            StoreId = storeId,
            EventType = eventType.ToString(),
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceType = deviceType,
            AdditionalData = additionalData,
            Success = success,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.AuditEvents.Add(auditEvent);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log persistence failed for event {EventType}", eventType);
            _context.ChangeTracker.Clear();
        }
    }
}

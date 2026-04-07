using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/admin/operation")]
public class BackofficeOperacionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IHashService _hashService;
    private readonly ISettingsService _settingsService;
    private readonly IEmergencyExportService _emergencyExportService;

    public BackofficeOperacionController(
        ApplicationDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        IHashService hashService,
        ISettingsService settingsService,
        IEmergencyExportService emergencyExportService)
    {
        _db = db;
        _config = config;
        _env = env;
        _hashService = hashService;
        _settingsService = settingsService;
        _emergencyExportService = emergencyExportService;
    }

    [HttpGet("system-status")]
    public async Task<ActionResult<object>> GetSystemStatus()
    {
        var dbOk = await _db.Database.CanConnectAsync();
        var apiVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

        var storagePath = GetFullPath(_config["App:StoragePath"] ?? "storage");
        var backupPath = GetFullPath(_config["App:BackupPath"] ?? "backups");
        var driveRoot = Path.GetPathRoot(storagePath) ?? storagePath;
        var drive = new DriveInfo(driveRoot);

        var lastBackupTimestamp = await ReadLastBackupTimestampAsync(backupPath);
        var offlineQueueCount = await _db.Sales.CountAsync(s => s.Status == SaleStatus.PendingTransfer.ToString());

        DateTime? lastSyncTimestamp = null;
        var syncFile = Path.Combine(storagePath, "last_sync.txt");
        if (System.IO.File.Exists(syncFile))
        {
            var raw = await System.IO.File.ReadAllTextAsync(syncFile);
            if (DateTime.TryParse(raw, out var parsed))
                lastSyncTimestamp = parsed.ToUniversalTime();
        }

        return Ok(new
        {
            dbOk,
            apiOk = true,
            apiVersion,
            diskFreeBytes = drive.AvailableFreeSpace,
            lastBackupTimestamp,
            offlineQueue = offlineQueueCount,
            lastSync = lastSyncTimestamp
        });
    }

    [HttpGet("checklist")]
    public async Task<ActionResult<List<ChecklistItemDto>>> GetChecklist()
    {
        var path = GetChecklistPath();
        if (!System.IO.File.Exists(path))
        {
            var seed = GetSeedChecklist();
            await SaveChecklistAsync(path, seed);
            return Ok(seed);
        }

        var json = await System.IO.File.ReadAllTextAsync(path);
        var items = JsonSerializer.Deserialize<List<ChecklistItemDto>>(json) ?? GetSeedChecklist();
        return Ok(items);
    }

    [HttpPut("checklist/{key}")]
    public async Task<ActionResult<List<ChecklistItemDto>>> ToggleChecklist(string key, [FromBody] ChecklistToggleRequest request)
    {
        var path = GetChecklistPath();
        List<ChecklistItemDto> items;

        if (System.IO.File.Exists(path))
        {
            var json = await System.IO.File.ReadAllTextAsync(path);
            items = JsonSerializer.Deserialize<List<ChecklistItemDto>>(json) ?? GetSeedChecklist();
        }
        else
        {
            items = GetSeedChecklist();
        }

        var item = items.FirstOrDefault(i => i.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (item == null)
            return NotFound(new { message = "Checklist item not found" });

        item.Done = request.Done;
        item.UpdatedAt = DateTime.UtcNow;

        await SaveChecklistAsync(path, items);
        return Ok(items);
    }

    [HttpGet("local-info")]
    public async Task<ActionResult<object>> GetLocalInfo()
    {
        var path = Path.Combine(GetFullPath(_config["App:StoragePath"] ?? "storage"), "startup-local-info.json");
        if (!System.IO.File.Exists(path))
            return Ok(new { localName = "", address = "", phone = "" });

        var json = await System.IO.File.ReadAllTextAsync(path);
        return Content(json, "application/json");
    }

    [HttpPost("local-info")]
    public async Task<ActionResult> SaveLocalInfo([FromBody] LocalInfoRequest request)
    {
        var path = Path.Combine(GetFullPath(_config["App:StoragePath"] ?? "storage"), "startup-local-info.json");
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(path, json);
        return Ok(new { message = "Local info saved" });
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<object>>> GetUsers()
    {
        var users = await _db.Usuarios
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, u.Role, u.IsActive, u.CreatedAt })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<ActionResult<object>> CreateUser([FromBody] CreateStartupUserRequest request)
    {
        var exists = await _db.Usuarios.AnyAsync(u => u.Username == request.Username);
        if (exists)
            return Conflict(new { message = "Username already exists" });

        var user = new Usuario
        {
            Username = request.Username,
            PasswordHash = _hashService.HashSha256(request.Password),
            PinHash = string.IsNullOrWhiteSpace(request.Pin) ? null : _hashService.HashSha256(request.Pin),
            Role = string.IsNullOrWhiteSpace(request.Role) ? UserRole.Operator.ToString() : request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.Role, user.IsActive });
    }

    [HttpGet("devices")]
    public async Task<ActionResult<List<object>>> GetDevices()
    {
        var devices = await _db.Devices
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                d.Id,
                d.DeviceName,
                d.DeviceType,
                d.ParentCashRegisterDeviceId,
                d.UsuarioId,
                d.CreatedAt,
                d.IsRevoked
            })
            .ToListAsync();

        return Ok(devices);
    }

    [HttpPost("devices")]
    public async Task<ActionResult<object>> CreateDevice([FromBody] CreateStartupDeviceRequest request)
    {
        var usuarioId = request.UsuarioId;
        if (usuarioId == null)
        {
            var fallbackUser = await _db.Usuarios.OrderBy(u => u.Id).FirstOrDefaultAsync();
            if (fallbackUser == null)
                return BadRequest(new { message = "No users found. Create user first." });
            usuarioId = fallbackUser.Id;
        }

        var rawToken = _hashService.GenerateToken();
        var tokenHash = _hashService.HashSha256(rawToken);

        var device = new Device
        {
            UsuarioId = usuarioId.Value,
            StoreId = request.StoreId,
            TokenHash = tokenHash,
            DeviceName = request.DeviceName,
            DeviceType = request.DeviceType,
            IpAddress = request.IpAddress,
            UserAgent = "Backoffice-Startup",
            ParentCashRegisterDeviceId = request.ParentCashRegisterDeviceId,
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            device.Id,
            device.DeviceName,
            device.DeviceType,
            device.ParentCashRegisterDeviceId,
            token = rawToken
        });
    }

    [HttpGet("settings/pos")]
    public async Task<ActionResult<Dictionary<string, string>>> GetPosSettings()
    {
        var settings = await _settingsService.GetPOSSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("settings/pos")]
    public async Task<ActionResult> UpdatePosSettings([FromBody] POSSettingsRequest request)
    {
        await _settingsService.UpdatePOSSettingsAsync(
            request.BigPurchaseMinAmount,
            request.BigPurchaseDiscountCapPercent,
            request.CigaretteSurchargePercent,
            request.CigaretteSurchargeMethods,
            request.LateFeeEnabled,
            request.LateFeePercentMonthly
        );

        return Ok(new { message = "Settings updated" });
    }

    [HttpGet("downloads/manual-kit")]
    public async Task<IActionResult> DownloadManualKit()
    {
        var tenantId = HttpContext.Items.TryGetValue("TenantId", out var tenantObj) && tenantObj is int t ? t : (int?)null;
        var brand = tenantId.HasValue
            ? await _db.TenantBrandingSettings.FirstOrDefaultAsync(b => b.TenantId == tenantId.Value)
            : null;

        var title = brand?.DisplayName ?? "Kodvian SuperMarket";
        var support = string.Join(" | ", new[]
        {
            string.IsNullOrWhiteSpace(brand?.SupportPhone) ? null : $"Soporte: {brand!.SupportPhone}",
            string.IsNullOrWhiteSpace(brand?.SupportEmail) ? null : $"Email: {brand!.SupportEmail}"
        }.Where(x => x != null));

        var text = $"{title} - Manual + Kit de Puesta en Marcha\n\n1) Verificar red\n2) Configurar dispositivos\n3) Crear usuarios\n4) Ejecutar pruebas";
        if (!string.IsNullOrWhiteSpace(support))
            text += $"\n\n{support}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        return File(bytes, "text/plain", "manual-kit.txt");
    }

    [HttpGet("downloads/emergency-catalog")]
    public async Task<IActionResult> DownloadEmergencyCatalog([FromQuery] int top = 100)
    {
        var tenantId = HttpContext.Items.TryGetValue("TenantId", out var tenantObj) && tenantObj is int t ? t : (int?)null;
        var bytes = await _emergencyExportService.GenerateEmergencyCatalogPdfAsync(top, true, true, tenantId);
        return File(bytes, "application/pdf", $"emergency-catalog-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
    }

    private string GetChecklistPath()
    {
        var storage = GetFullPath(_config["App:StoragePath"] ?? "storage");
        return Path.Combine(storage, "startup-checklist.json");
    }

    private static List<ChecklistItemDto> GetSeedChecklist() => new()
    {
        new ChecklistItemDto { Key = "network", Label = "Red local estable", Done = false },
        new ChecklistItemDto { Key = "cash_register", Label = "Caja configurada", Done = false },
        new ChecklistItemDto { Key = "tablets", Label = "Tablets asociadas", Done = false },
        new ChecklistItemDto { Key = "users", Label = "Usuarios y PIN creados", Done = false },
        new ChecklistItemDto { Key = "settings", Label = "Parametros clave cargados", Done = false },
        new ChecklistItemDto { Key = "exports", Label = "Manual y catalogo descargados", Done = false },
        new ChecklistItemDto { Key = "tests", Label = "Pruebas operativas completas", Done = false }
    };

    private static async Task SaveChecklistAsync(string path, List<ChecklistItemDto> items)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(path, json);
    }

    private static async Task<DateTime?> ReadLastBackupTimestampAsync(string backupPath)
    {
        var lastBackupFile = Path.Combine(backupPath, "last_backup.txt");
        if (System.IO.File.Exists(lastBackupFile))
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(lastBackupFile);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("timestamp", out var tsEl)
                    && DateTime.TryParse(tsEl.GetString(), out var parsed))
                {
                    return parsed.ToUniversalTime();
                }
            }
            catch
            {
            }
        }

        if (!Directory.Exists(backupPath))
            return null;

        var latest = Directory
            .EnumerateFiles(backupPath)
            .Where(f => f.EndsWith(".dump", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                || f.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .FirstOrDefault();

        return latest?.LastWriteTimeUtc;
    }

    private string GetFullPath(string path)
    {
        var full = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(_env.ContentRootPath, path));

        Directory.CreateDirectory(full);
        return full;
    }
}

public class ChecklistItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Done { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ChecklistToggleRequest
{
    public bool Done { get; set; }
}

public class LocalInfoRequest
{
    public string LocalName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class CreateStartupUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Pin { get; set; }
    public string Role { get; set; } = UserRole.Operator.ToString();
}

public class CreateStartupDeviceRequest
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Tablet";
    public string? IpAddress { get; set; }
    public int? ParentCashRegisterDeviceId { get; set; }
    public int? UsuarioId { get; set; }
    public int? StoreId { get; set; }
}

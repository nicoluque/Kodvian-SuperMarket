using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/admin")]
[DeviceAuth]
[OperatorSessionAuth]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AdminController(ApplicationDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    [HttpGet("system-status")]
    public async Task<ActionResult<object>> GetSystemStatus()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var userIdObj) || userIdObj is not int userId)
            return Unauthorized();

        var user = await _db.Usuarios.FindAsync(userId);
        if (user == null)
            return Unauthorized();

        if (user.Role != UserRole.Admin.ToString() && user.Role != UserRole.Supervisor.ToString() && user.Role != "Manager")
            return Forbid();

        var dbOk = await _db.Database.CanConnectAsync();

        var storagePath = _config["App:StoragePath"] ?? "storage";
        var backupPath = _config["App:BackupPath"] ?? "backups";

        var storageFullPath = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.GetFullPath(Path.Combine(_env.ContentRootPath, storagePath));

        var backupFullPath = Path.IsPathRooted(backupPath)
            ? backupPath
            : Path.GetFullPath(Path.Combine(_env.ContentRootPath, backupPath));

        var driveRoot = Path.GetPathRoot(storageFullPath) ?? storageFullPath;
        var storageOk = true;
        DriveInfo? drive = null;
        try
        {
            drive = new DriveInfo(driveRoot);
            _ = drive.AvailableFreeSpace;
        }
        catch
        {
            storageOk = false;
        }

        DateTime? lastBackupTimestamp = null;
        var lastBackupFile = Path.Combine(backupFullPath, "last_backup.txt");

        if (System.IO.File.Exists(lastBackupFile))
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(lastBackupFile);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("timestamp", out var tsEl)
                    && DateTime.TryParse(tsEl.GetString(), out var parsed))
                {
                    lastBackupTimestamp = parsed.ToUniversalTime();
                }
            }
            catch
            {
            }
        }

        if (lastBackupTimestamp == null && Directory.Exists(backupFullPath))
        {
            var latest = Directory
                .EnumerateFiles(backupFullPath)
                .Where(f => f.EndsWith(".dump", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                    || f.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latest != null)
                lastBackupTimestamp = latest.LastWriteTimeUtc;
        }

        var apiVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
        var offlineQueuePending = await _db.Sales.CountAsync(s => s.Status == SaleStatus.PendingTransfer.ToString());

        return Ok(new
        {
            dbOk,
            apiVersion,
            storageOk,
            disk = new
            {
                path = storageFullPath,
                freeBytes = drive?.AvailableFreeSpace ?? 0,
                totalBytes = drive?.TotalSize ?? 0
            },
            lastBackupTimestamp,
            offlineQueuePending
        });
    }
}

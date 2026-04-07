using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1")]
[OperatorSessionAuth]
public class MultiLocalAdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public MultiLocalAdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<List<object>>> GetTenants()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var data = await _db.Tenants.OrderBy(t => t.Name).Select(t => new { t.Id, t.Name, t.Code, t.IsActive, t.CreatedAt }).ToListAsync();
        return Ok(data);
    }

    [HttpPost("tenants")]
    public async Task<ActionResult<object>> CreateTenant([FromBody] TenantUpsertRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var t = new Tenant { Name = req.Name, Code = req.Code, IsActive = req.IsActive, CreatedAt = DateTime.UtcNow };
        _db.Tenants.Add(t);
        await _db.SaveChangesAsync();
        return Ok(new { t.Id, t.Name, t.Code, t.IsActive });
    }

    [HttpPut("tenants/{id}")]
    public async Task<ActionResult<object>> UpdateTenant(int id, [FromBody] TenantUpsertRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var t = await _db.Tenants.FindAsync(id);
        if (t == null) return NotFound();
        t.Name = req.Name; t.Code = req.Code; t.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { t.Id, t.Name, t.Code, t.IsActive });
    }

    [HttpGet("stores")]
    public async Task<ActionResult<List<object>>> GetStores([FromQuery] int? tenantId = null)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var q = _db.Stores.AsQueryable();
        if (tenantId.HasValue) q = q.Where(s => s.TenantId == tenantId.Value);
        var data = await q.OrderBy(s => s.Name).Select(s => new { s.Id, s.TenantId, s.Name, s.Code, s.Address, s.Phone, s.IsActive, s.SettingsJson, s.CreatedAt }).ToListAsync();
        return Ok(data);
    }

    [HttpGet("stores/my")]
    public async Task<ActionResult<List<object>>> GetMyStores()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var uidObj) || uidObj is not int usuarioId)
            return Unauthorized();

        var user = await _db.Usuarios.FindAsync(usuarioId);
        if (user == null) return Unauthorized();

        if (user.Role == UserRole.Admin.ToString() || user.Role == UserRole.Supervisor.ToString() || user.Role == "Manager")
        {
            var all = await _db.Stores.Where(s => s.IsActive).OrderBy(s => s.Name).Select(s => new { s.Id, s.Name, s.TenantId }).ToListAsync();
            return Ok(all);
        }

        var stores = await _db.StoreUsers
            .Where(su => su.UsuarioId == usuarioId && su.IsActive)
            .Select(su => new { su.Store.Id, su.Store.Name, su.Store.TenantId })
            .Distinct()
            .ToListAsync();
        return Ok(stores);
    }

    [HttpPost("stores")]
    public async Task<ActionResult<object>> CreateStore([FromBody] StoreUpsertRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var s = new Store { TenantId = req.TenantId, Name = req.Name, Code = req.Code, Address = req.Address, Phone = req.Phone, IsActive = req.IsActive, CreatedAt = DateTime.UtcNow };
        _db.Stores.Add(s);
        await _db.SaveChangesAsync();
        return Ok(new { s.Id, s.TenantId, s.Name, s.Code, s.Address, s.Phone, s.IsActive });
    }

    [HttpPut("stores/{id}")]
    public async Task<ActionResult<object>> UpdateStore(int id, [FromBody] StoreUpsertRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var s = await _db.Stores.FindAsync(id);
        if (s == null) return NotFound();
        s.TenantId = req.TenantId; s.Name = req.Name; s.Code = req.Code; s.Address = req.Address; s.Phone = req.Phone; s.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { s.Id, s.TenantId, s.Name, s.Code, s.Address, s.Phone, s.IsActive });
    }

    [HttpGet("stores/{id}/settings")]
    public async Task<ActionResult<object>> GetStoreSettings(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var s = await _db.Stores.FindAsync(id);
        if (s == null) return NotFound();
        return Content(s.SettingsJson ?? "{}", "application/json");
    }

    [HttpPut("stores/{id}/settings")]
    public async Task<ActionResult> UpdateStoreSettings(int id, [FromBody] object payload)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var s = await _db.Stores.FindAsync(id);
        if (s == null) return NotFound();
        s.SettingsJson = System.Text.Json.JsonSerializer.Serialize(payload);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Store settings updated" });
    }

    [HttpGet("stores/{id}/shift-config")]
    public async Task<ActionResult<object>> GetShiftConfig(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var s = await _db.Stores.FindAsync(id);
        if (s == null) return NotFound();

        var shift = new Dictionary<string, object?>
        {
            ["timezone"] = "America/Argentina/Buenos_Aires",
            ["morningStart"] = "07:30",
            ["morningCloseWindowStart"] = "14:00",
            ["morningCloseWindowEnd"] = "15:00",
            ["afternoonEnd"] = "22:00",
            ["graceMinutes"] = 90
        };

        try
        {
            using var doc = JsonDocument.Parse(s.SettingsJson ?? "{}");
            if (doc.RootElement.TryGetProperty("shiftSchedule", out var shiftEl))
            {
                if (shiftEl.TryGetProperty("timezone", out var timezoneEl) && timezoneEl.ValueKind == JsonValueKind.String)
                    shift["timezone"] = timezoneEl.GetString();
                if (shiftEl.TryGetProperty("morningStart", out var morningStartEl) && morningStartEl.ValueKind == JsonValueKind.String)
                    shift["morningStart"] = morningStartEl.GetString();
                if (shiftEl.TryGetProperty("morningCloseWindowStart", out var closeStartEl) && closeStartEl.ValueKind == JsonValueKind.String)
                    shift["morningCloseWindowStart"] = closeStartEl.GetString();
                if (shiftEl.TryGetProperty("morningCloseWindowEnd", out var closeEndEl) && closeEndEl.ValueKind == JsonValueKind.String)
                    shift["morningCloseWindowEnd"] = closeEndEl.GetString();
                if (shiftEl.TryGetProperty("afternoonEnd", out var afternoonEndEl) && afternoonEndEl.ValueKind == JsonValueKind.String)
                    shift["afternoonEnd"] = afternoonEndEl.GetString();
                if (shiftEl.TryGetProperty("graceMinutes", out var graceEl) && graceEl.TryGetInt32(out var grace))
                    shift["graceMinutes"] = grace;
            }
        }
        catch
        {
        }

        return Ok(shift);
    }

    [HttpPut("stores/{id}/shift-config")]
    public async Task<ActionResult> UpdateShiftConfig(int id, [FromBody] StoreShiftConfigUpsertRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var s = await _db.Stores.FindAsync(id);
        if (s == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.MorningStart)
            || string.IsNullOrWhiteSpace(req.MorningCloseWindowStart)
            || string.IsNullOrWhiteSpace(req.MorningCloseWindowEnd)
            || string.IsNullOrWhiteSpace(req.AfternoonEnd))
            return BadRequest(new { message = "Shift schedule values are required" });

        var root = JsonNode.Parse(s.SettingsJson ?? "{}") as JsonObject ?? new JsonObject();
        root["shiftSchedule"] = new JsonObject
        {
            ["timezone"] = string.IsNullOrWhiteSpace(req.Timezone) ? "America/Argentina/Buenos_Aires" : req.Timezone,
            ["morningStart"] = req.MorningStart,
            ["morningCloseWindowStart"] = req.MorningCloseWindowStart,
            ["morningCloseWindowEnd"] = req.MorningCloseWindowEnd,
            ["afternoonEnd"] = req.AfternoonEnd,
            ["graceMinutes"] = req.GraceMinutes <= 0 ? 90 : req.GraceMinutes
        };

        s.SettingsJson = root.ToJsonString();
        await _db.SaveChangesAsync();
        return Ok(new { message = "Shift config updated" });
    }

    [HttpGet("stores/{id}/users")]
    public async Task<ActionResult<List<object>>> GetStoreUsers(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var users = await _db.StoreUsers
            .Where(su => su.StoreId == id)
            .Select(su => new { su.Id, su.StoreId, su.UsuarioId, username = su.Usuario.Username, su.Role, su.IsActive })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("stores/{id}/users")]
    public async Task<ActionResult<object>> AddStoreUser(int id, [FromBody] StoreUserUpsertRequest req)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var existing = await _db.StoreUsers.FirstOrDefaultAsync(su => su.StoreId == id && su.UsuarioId == req.UsuarioId);
        if (existing == null)
        {
            existing = new StoreUser { StoreId = id, UsuarioId = req.UsuarioId, Role = req.Role, IsActive = req.IsActive, CreatedAt = DateTime.UtcNow };
            _db.StoreUsers.Add(existing);
        }
        else
        {
            existing.Role = req.Role;
            existing.IsActive = req.IsActive;
        }
        await _db.SaveChangesAsync();
        return Ok(new { existing.Id, existing.StoreId, existing.UsuarioId, existing.Role, existing.IsActive });
    }

    [HttpDelete("stores/{id}/users")]
    public async Task<ActionResult> RemoveStoreUser(int id, [FromQuery] int usuarioId)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var rel = await _db.StoreUsers.FirstOrDefaultAsync(su => su.StoreId == id && su.UsuarioId == usuarioId);
        if (rel == null) return NotFound();
        _db.StoreUsers.Remove(rel);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Store user removed" });
    }

    private async Task<bool> IsAdminOrManagerAsync()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var userIdObj) || userIdObj is not int userId)
            return false;

        var user = await _db.Usuarios.FindAsync(userId);
        if (user == null)
            return false;

        return user.Role == UserRole.Admin.ToString() || user.Role == UserRole.Supervisor.ToString() || user.Role == "Manager";
    }

}

public class TenantUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StoreUpsertRequest
{
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StoreUserUpsertRequest
{
    public int UsuarioId { get; set; }
    public string Role { get; set; } = UserRole.Operator.ToString();
    public bool IsActive { get; set; } = true;
}

public class StoreShiftConfigUpsertRequest
{
    public string Timezone { get; set; } = "America/Argentina/Buenos_Aires";
    public string MorningStart { get; set; } = "07:30";
    public string MorningCloseWindowStart { get; set; } = "14:00";
    public string MorningCloseWindowEnd { get; set; } = "15:00";
    public string AfternoonEnd { get; set; } = "22:00";
    public int GraceMinutes { get; set; } = 90;
}

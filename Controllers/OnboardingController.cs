using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/onboarding")]
[DeviceAuth]
[OperatorSessionAuth]
public class OnboardingController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    private static readonly (string key, string title, string[] endpoints)[] StepCatalog =
    {
        ("comercio", "Comercio", new[] {"/api/v1/tenants", "/api/v1/stores"}),
        ("local", "Local", new[] {"/api/v1/stores", "/api/v1/stores/{id}/settings"}),
        ("configuracion-base", "Configuracion base", new[] {"/api/v1/admin/operation/settings/pos"}),
        ("dispositivos", "Dispositivos", new[] {"/api/v1/devices", "/api/v1/admin/operation/devices"}),
        ("usuarios-pin", "Usuarios y PIN", new[] {"/api/v1/admin/operation/users", "/api/v1/stores/{id}/users"}),
        ("importaciones", "Importaciones", new[] {"/api/v1/import/products/preview", "/api/v1/import/products/commit"}),
        ("configuracion-operativa", "Configuracion operativa", new[] {"/api/v1/stores/{id}/settings", "/api/v1/admin/operation/checklist"}),
        ("pruebas-operativas", "Pruebas operativas", new[] {"/api/v1/admin/operation/checklist"}),
        ("contingencia", "Contingencia", new[] {"/api/v1/admin/operation/downloads/manual-kit", "/api/v1/admin/operation/downloads/emergency-catalog"}),
        ("resumen-final", "Resumen final", new[] {"/api/v1/onboarding/{id}/complete"})
    };

    public OnboardingController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("start")]
    public async Task<ActionResult<object>> Start([FromBody] StartOnboardingRequest request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var usuarioId = GetUsuarioId();
        if (!usuarioId.HasValue) return Unauthorized();

        var existing = await _db.OnboardingSessions
            .Include(s => s.Steps)
            .FirstOrDefaultAsync(s => s.CreatedByUsuarioId == usuarioId.Value && s.Status == OnboardingStatus.InProgress.ToString());

        if (existing != null)
            return Ok(ToSessionDto(existing));

        var session = new OnboardingSession
        {
            TenantId = request.TenantId,
            StoreId = request.StoreId,
            CreatedByUsuarioId = usuarioId.Value,
            Status = OnboardingStatus.InProgress.ToString(),
            CurrentStepKey = StepCatalog[0].key,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.OnboardingSessions.Add(session);
        await _db.SaveChangesAsync();

        foreach (var step in StepCatalog)
        {
            _db.OnboardingStepStates.Add(new OnboardingStepState
            {
                OnboardingSessionId = session.Id,
                StepKey = step.key,
                IsCompleted = false
            });
        }

        await _db.SaveChangesAsync();

        session = await _db.OnboardingSessions.Include(s => s.Steps).FirstAsync(s => s.Id == session.Id);
        return Ok(ToSessionDto(session));
    }

    [HttpGet("current")]
    public async Task<ActionResult<object>> Current()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var usuarioId = GetUsuarioId();
        if (!usuarioId.HasValue) return Unauthorized();

        var session = await _db.OnboardingSessions
            .Include(s => s.Steps)
            .Where(s => s.CreatedByUsuarioId == usuarioId.Value)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session == null)
            return NotFound(new { message = "No onboarding session found" });

        return Ok(ToSessionDto(session));
    }

    [HttpPut("{id}/step")]
    public async Task<ActionResult<object>> UpdateStep(int id, [FromBody] UpdateCurrentStepRequest request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var session = await _db.OnboardingSessions.Include(s => s.Steps).FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        if (!StepCatalog.Any(s => s.key == request.StepKey))
            return BadRequest(new { message = "Invalid stepKey" });

        session.CurrentStepKey = request.StepKey;
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToSessionDto(session));
    }

    [HttpGet("{id}/steps")]
    public async Task<ActionResult<List<object>>> Steps(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var session = await _db.OnboardingSessions.Include(s => s.Steps).FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        var states = session.Steps.ToDictionary(s => s.StepKey, s => s);
        var list = StepCatalog.Select((step, index) => new
        {
            index = index + 1,
            stepKey = step.key,
            title = step.title,
            isCompleted = states.TryGetValue(step.key, out var st) && st.IsCompleted,
            completedAt = states.TryGetValue(step.key, out st) ? st.CompletedAt : null,
            endpoints = step.endpoints
        }).Cast<object>().ToList();

        return Ok(list);
    }

    [HttpPost("{id}/steps/{stepKey}/complete")]
    public async Task<ActionResult<object>> CompleteStep(int id, string stepKey, [FromBody] CompleteStepRequest request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var session = await _db.OnboardingSessions.Include(s => s.Steps).FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        var step = session.Steps.FirstOrDefault(s => s.StepKey == stepKey);
        if (step == null)
            return NotFound(new { message = "Step not found" });

        step.IsCompleted = true;
        step.CompletedAt = DateTime.UtcNow;
        step.DataJson = request.DataJson;

        var idx = Array.FindIndex(StepCatalog, s => s.key == stepKey);
        if (idx >= 0 && idx < StepCatalog.Length - 1)
            session.CurrentStepKey = StepCatalog[idx + 1].key;

        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToSessionDto(session));
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<object>> Complete(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var session = await _db.OnboardingSessions.Include(s => s.Steps).FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        session.Status = OnboardingStatus.Completed.ToString();
        session.CompletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.CurrentStepKey = StepCatalog[^1].key;
        await _db.SaveChangesAsync();

        return Ok(ToSessionDto(session));
    }

    private object ToSessionDto(OnboardingSession s)
    {
        return new
        {
            s.Id,
            s.TenantId,
            s.StoreId,
            s.Status,
            s.CurrentStepKey,
            s.CreatedByUsuarioId,
            s.CreatedAt,
            s.UpdatedAt,
            s.CompletedAt,
            completedSteps = s.Steps.Count(x => x.IsCompleted),
            totalSteps = StepCatalog.Length
        };
    }

    private int? GetUsuarioId()
    {
        return HttpContext.Items.TryGetValue("SessionUsuarioId", out var userIdObj) && userIdObj is int userId
            ? userId
            : (int?)null;
    }

    private async Task<bool> IsAdminOrManagerAsync()
    {
        var userId = GetUsuarioId();
        if (!userId.HasValue) return false;
        var user = await _db.Usuarios.FindAsync(userId.Value);
        if (user == null) return false;
        return user.Role == UserRole.Admin.ToString() || user.Role == UserRole.Supervisor.ToString() || user.Role == "Manager";
    }
}

public class StartOnboardingRequest
{
    public int? TenantId { get; set; }
    public int? StoreId { get; set; }
}

public class UpdateCurrentStepRequest
{
    public string StepKey { get; set; } = string.Empty;
}

public class CompleteStepRequest
{
    public string? DataJson { get; set; }
}

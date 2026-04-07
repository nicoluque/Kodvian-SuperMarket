using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/training")]
public class TrainingController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITrainingService _trainingService;

    public TrainingController(ApplicationDbContext db, ITrainingService trainingService)
    {
        _db = db;
        _trainingService = trainingService;
    }

    [HttpGet("checklists")]
    public async Task<ActionResult<object>> GetChecklists()
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue)
            return Ok(new List<object>());

        await _trainingService.EnsureSeededAsync(tenantId.Value);

        var checklists = await _db.TrainingChecklists
            .Include(c => c.Items)
            .Where(c => c.TenantId == tenantId.Value && c.IsActive)
            .OrderBy(c => c.Role)
            .ThenBy(c => c.Id)
            .ToListAsync();

        var latestRuns = await _db.TrainingChecklistRuns
            .Where(r => r.TenantId == tenantId.Value)
            .GroupBy(r => r.Role)
            .Select(g => g.OrderByDescending(x => x.StartedAt).First())
            .ToListAsync();

        var data = checklists
            .GroupBy(c => c.Role)
            .Select(g =>
            {
                var role = g.Key;
                var totalItems = g.Sum(x => x.Items.Count);
                var latest = latestRuns.FirstOrDefault(x => x.Role == role);
                return new
                {
                    role,
                    checklistCount = g.Count(),
                    totalItems,
                    latestRunId = latest?.Id,
                    latestRunStatus = latest?.Status,
                    latestRunAt = latest?.StartedAt,
                    latestCompletedAt = latest?.CompletedAt
                };
            })
            .OrderBy(x => x.role)
            .Cast<object>()
            .ToList();

        return Ok(data);
    }

    [HttpGet("checklists/{role}")]
    public async Task<ActionResult<object>> GetChecklistByRole(string role)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue)
            return NotFound(new { message = "Training tenant not found" });

        await _trainingService.EnsureSeededAsync(tenantId.Value);

        var normalizedRole = role.Trim();
        var checklists = await _db.TrainingChecklists
            .Include(c => c.Items.OrderBy(i => i.SortOrder))
            .Where(c => c.TenantId == tenantId.Value && c.IsActive && c.Role.ToLower() == normalizedRole.ToLower())
            .OrderBy(c => c.Id)
            .ToListAsync();

        if (checklists.Count == 0)
            return NotFound(new { message = "Checklist role not found" });

        var latestRun = await _db.TrainingChecklistRuns
            .Where(r => r.TenantId == tenantId.Value && r.Role.ToLower() == normalizedRole.ToLower())
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();

        var latestRunItems = latestRun == null
            ? new List<TrainingChecklistRunItem>()
            : await _db.TrainingChecklistRunItems.Where(i => i.TrainingChecklistRunId == latestRun.Id).ToListAsync();

        var payload = new
        {
            role = checklists[0].Role,
            checklists = checklists.Select(c => new
            {
                id = c.Id,
                title = c.Title,
                description = c.Description,
                totalItems = c.Items.Count,
                completedItems = latestRunItems.Count(i => i.IsCompleted && c.Items.Any(ci => ci.Id == i.TrainingChecklistItemId)),
                items = c.Items.OrderBy(i => i.SortOrder).Select(i => new
                {
                    id = i.Id,
                    title = i.Title,
                    description = i.Description,
                    sortOrder = i.SortOrder,
                    isRequired = i.IsRequired,
                    isCompleted = latestRunItems.Any(ri => ri.TrainingChecklistItemId == i.Id && ri.IsCompleted)
                })
            }),
            latestRun = latestRun == null ? null : new
            {
                id = latestRun.Id,
                status = latestRun.Status,
                startedAt = latestRun.StartedAt,
                completedAt = latestRun.CompletedAt
            }
        };

        return Ok(payload);
    }

    [HttpPost("runs/start")]
    public async Task<ActionResult<object>> StartRun([FromBody] StartTrainingRunRequest request)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue)
            return NotFound(new { message = "Training tenant not found" });

        await _trainingService.EnsureSeededAsync(tenantId.Value);

        TrainingChecklist? checklist = null;
        if (request.ChecklistId.HasValue)
        {
            checklist = await _db.TrainingChecklists
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == request.ChecklistId.Value && c.TenantId == tenantId.Value && c.IsActive);
        }
        else if (!string.IsNullOrWhiteSpace(request.Role))
        {
            checklist = await _db.TrainingChecklists
                .Include(c => c.Items)
                .Where(c => c.TenantId == tenantId.Value && c.IsActive && c.Role.ToLower() == request.Role.Trim().ToLower())
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync();
        }

        if (checklist == null)
            return BadRequest(new { message = "Checklist not found for requested role/id" });

        if (checklist.Items.Count == 0)
            return BadRequest(new { message = "Checklist has no items" });

        var startedByUserId = HttpContext.Items.TryGetValue("SessionUsuarioId", out var userObj) && userObj is int uid ? uid : (int?)null;

        var run = new TrainingChecklistRun
        {
            TenantId = tenantId.Value,
            TrainingChecklistId = checklist.Id,
            Role = checklist.Role,
            StartedByUsuarioId = startedByUserId,
            Status = TrainingRunStatus.InProgress.ToString(),
            StartedAt = DateTime.UtcNow
        };
        _db.TrainingChecklistRuns.Add(run);
        await _db.SaveChangesAsync();

        _db.TrainingChecklistRunItems.AddRange(checklist.Items.Select(i => new TrainingChecklistRunItem
        {
            TrainingChecklistRunId = run.Id,
            TrainingChecklistItemId = i.Id,
            IsCompleted = false
        }));

        await _db.SaveChangesAsync();

        return Ok(new
        {
            runId = run.Id,
            role = run.Role,
            checklistId = checklist.Id,
            totalItems = checklist.Items.Count,
            startedAt = run.StartedAt,
            status = run.Status
        });
    }

    [HttpPost("runs/{id}/items/{itemId}/complete")]
    public async Task<ActionResult<object>> CompleteRunItem(int id, int itemId)
    {
        var run = await _db.TrainingChecklistRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (run == null)
            return NotFound(new { message = "Run not found" });

        var runItem = await _db.TrainingChecklistRunItems
            .FirstOrDefaultAsync(i => i.TrainingChecklistRunId == id && i.TrainingChecklistItemId == itemId);

        if (runItem == null)
            return NotFound(new { message = "Run item not found" });

        if (!runItem.IsCompleted)
        {
            runItem.IsCompleted = true;
            runItem.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        var total = await _db.TrainingChecklistRunItems.CountAsync(i => i.TrainingChecklistRunId == id);
        var completed = await _db.TrainingChecklistRunItems.CountAsync(i => i.TrainingChecklistRunId == id && i.IsCompleted);

        if (total > 0 && completed == total && run.Status != TrainingRunStatus.Completed.ToString())
        {
            run.Status = TrainingRunStatus.Completed.ToString();
            run.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            runId = id,
            itemId,
            completedItems = completed,
            totalItems = total,
            progress = total == 0 ? 0 : Math.Round((completed * 100m) / total, 2),
            runStatus = run.Status,
            runCompletedAt = run.CompletedAt
        });
    }

    [HttpGet("runs/history")]
    public async Task<ActionResult<List<object>>> History([FromQuery] string? role = null, [FromQuery] int top = 50)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (!tenantId.HasValue)
            return Ok(new List<object>());

        if (top <= 0) top = 50;
        if (top > 200) top = 200;

        var query = _db.TrainingChecklistRuns
            .Include(r => r.StartedByUsuario)
            .Where(r => r.TenantId == tenantId.Value)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(r => r.Role.ToLower() == role.Trim().ToLower());

        var runs = await query
            .OrderByDescending(r => r.StartedAt)
            .Take(top)
            .ToListAsync();

        var runIds = runs.Select(r => r.Id).ToList();
        var runItems = await _db.TrainingChecklistRunItems
            .Where(i => runIds.Contains(i.TrainingChecklistRunId))
            .ToListAsync();

        var data = runs.Select(r =>
        {
            var total = runItems.Count(i => i.TrainingChecklistRunId == r.Id);
            var completedCount = runItems.Count(i => i.TrainingChecklistRunId == r.Id && i.IsCompleted);
            return (object)new
            {
                id = r.Id,
                role = r.Role,
                checklistId = r.TrainingChecklistId,
                status = r.Status,
                startedAt = r.StartedAt,
                completedAt = r.CompletedAt,
                startedByUsuarioId = r.StartedByUsuarioId,
                startedByUsername = r.StartedByUsuario?.Username,
                completedItems = completedCount,
                totalItems = total,
                progress = total == 0 ? 0 : Math.Round((completedCount * 100m) / total, 2)
            };
        }).ToList();

        return Ok(data);
    }

    private async Task<int?> ResolveTenantIdAsync()
    {
        var tenantFromHeader = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        int? tenantId = int.TryParse(tenantFromHeader, out var tid) ? tid : null;

        var storeFromHeader = Request.Headers["X-Store-Id"].FirstOrDefault();
        int? storeId = int.TryParse(storeFromHeader, out var sid) ? sid : null;

        return await _trainingService.ResolveTrainingTenantIdAsync(tenantId, storeId);
    }
}

public class StartTrainingRunRequest
{
    public string? Role { get; set; }
    public int? ChecklistId { get; set; }
}

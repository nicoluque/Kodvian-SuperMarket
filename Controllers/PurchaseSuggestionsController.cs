using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/purchase-suggestions")]
[OperatorSessionAuth]
public class PurchaseSuggestionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPurchaseSuggestionService _service;

    public PurchaseSuggestionsController(ApplicationDbContext db, IPurchaseSuggestionService service)
    {
        _db = db;
        _service = service;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<object>> Generate([FromBody] PurchaseSuggestionGenerateRequest? request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        request ??= new PurchaseSuggestionGenerateRequest();

        var storeId = ResolveStoreIdFromHeader();
        var tenantId = await ResolveTenantIdAsync(storeId);
        var userId = ResolveSessionUserId();

        var suggestion = await _service.GenerateAsync(
            tenantId,
            storeId,
            userId,
            request.DaysWindow,
            request.TargetCoverageDays,
            request.SupplierId,
            request.CriticalOnly
        );

        return Ok(ToDetailResponse(suggestion));
    }

    [HttpGet]
    public async Task<ActionResult<List<object>>> GetAll()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var storeId = ResolveStoreIdFromHeader();
        var tenantId = await ResolveTenantIdAsync(storeId);
        var suggestions = await _service.GetAllAsync(tenantId, storeId);

        var payload = suggestions.Select(s => (object)new
        {
            id = s.Id,
            status = s.Status,
            generatedAt = s.GeneratedAt,
            daysWindow = s.DaysWindow,
            targetCoverageDays = s.TargetCoverageDays,
            totalLines = s.Lines.Count,
            pendingLines = s.Lines.Count(l => l.Status == PurchaseSuggestionLineStatus.Pending.ToString()),
            acceptedLines = s.Lines.Count(l => l.Status == PurchaseSuggestionLineStatus.Accepted.ToString()),
            ignoredLines = s.Lines.Count(l => l.Status == PurchaseSuggestionLineStatus.Ignored.ToString()),
            convertedLines = s.Lines.Count(l => l.Status == PurchaseSuggestionLineStatus.Converted.ToString())
        }).ToList();

        return Ok(payload);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var suggestion = await _service.GetByIdAsync(id);
        if (suggestion == null) return NotFound();

        return Ok(ToDetailResponse(suggestion));
    }

    [HttpPut("{id}/lines/{lineId}")]
    public async Task<ActionResult<object>> UpdateLine(int id, int lineId, [FromBody] PurchaseSuggestionLineUpdateRequest request)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        try
        {
            var line = await _service.UpdateLineAsync(id, lineId, request.Status, request.AcceptedQty, request.Notes);
            return Ok(new
            {
                lineId = line.Id,
                status = line.Status,
                acceptedQty = line.AcceptedQty,
                notes = line.Notes
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/convert-to-purchase")]
    public async Task<ActionResult<object>> ConvertToPurchase(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();
        var deviceId = await ResolveDeviceIdAsync();
        var userId = await ResolveUsuarioIdAsync(deviceId);
        if (!deviceId.HasValue || !userId.HasValue)
            return BadRequest(new { message = "No se pudo resolver usuario/dispositivo para crear compras draft" });

        try
        {
            var purchaseIds = await _service.ConvertToPurchaseAsync(id, userId.Value, deviceId.Value);
            return Ok(new
            {
                suggestionId = id,
                purchasesCreated = purchaseIds.Count,
                purchaseIds
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<List<object>>> GetSuppliersForBackoffice()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var suppliers = await _db.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => (object)new
            {
                id = s.Id,
                name = s.Name,
                cuit = s.CUIT,
                phone = s.Phone,
                email = s.Email
            })
            .ToListAsync();

        return Ok(suppliers);
    }

    private object ToDetailResponse(PurchaseSuggestion s)
    {
        return new
        {
            id = s.Id,
            tenantId = s.TenantId,
            storeId = s.StoreId,
            status = s.Status,
            generatedAt = s.GeneratedAt,
            daysWindow = s.DaysWindow,
            targetCoverageDays = s.TargetCoverageDays,
            lines = s.Lines
                .OrderByDescending(l => l.SuggestedQty)
                .ThenBy(l => l.Product?.Name)
                .Select(l => new
                {
                    id = l.Id,
                    productId = l.ProductId,
                    productName = l.Product?.Name,
                    purchaseUnit = l.Product?.PurchaseUnit,
                    currentStock = l.CurrentStock,
                    minStock = l.MinStock,
                    avgDailySales = l.AvgDailySales,
                    targetCoverageStock = l.TargetCoverageStock,
                    suggestedQty = l.SuggestedQty,
                    acceptedQty = l.AcceptedQty,
                    supplierId = l.SuggestedSupplierId,
                    supplierName = l.SuggestedSupplier?.Name,
                    reason = l.Reason,
                    status = l.Status,
                    notes = l.Notes,
                    createdPurchaseId = l.CreatedPurchaseId,
                    isCritical = l.CurrentStock < l.MinStock
                })
        };
    }

    private int? ResolveSessionUserId()
    {
        return HttpContext.Items.TryGetValue("SessionUsuarioId", out var userObj) && userObj is int uid ? uid : null;
    }

    private int? ResolveStoreIdFromHeader()
    {
        var raw = Request.Headers["X-Store-Id"].FirstOrDefault();
        return int.TryParse(raw, out var id) ? id : null;
    }

    private async Task<int?> ResolveTenantIdAsync(int? storeId)
    {
        var raw = Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (int.TryParse(raw, out var tenantId)) return tenantId;

        if (storeId.HasValue)
            return await _db.Stores.Where(s => s.Id == storeId.Value).Select(s => (int?)s.TenantId).FirstOrDefaultAsync();

        return await _db.Tenants.OrderBy(t => t.Id).Select(t => (int?)t.Id).FirstOrDefaultAsync();
    }

    private async Task<int?> ResolveDeviceIdAsync()
    {
        if (HttpContext.Items.TryGetValue("DeviceId", out var deviceObj) && deviceObj is int did)
            return did;

        var storeId = ResolveStoreIdFromHeader();
        if (storeId.HasValue)
            return await _db.Devices.Where(d => d.StoreId == storeId.Value && !d.IsRevoked).OrderBy(d => d.Id).Select(d => (int?)d.Id).FirstOrDefaultAsync();

        return await _db.Devices.Where(d => !d.IsRevoked).OrderBy(d => d.Id).Select(d => (int?)d.Id).FirstOrDefaultAsync();
    }

    private async Task<int?> ResolveUsuarioIdAsync(int? deviceId)
    {
        if (HttpContext.Items.TryGetValue("SessionUsuarioId", out var userObj) && userObj is int uid)
            return uid;

        if (deviceId.HasValue)
            return await _db.Devices.Where(d => d.Id == deviceId.Value).Select(d => (int?)d.UsuarioId).FirstOrDefaultAsync();

        return await _db.Usuarios.Where(u => u.IsActive).OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync();
    }

    private async Task<bool> IsAdminOrManagerAsync()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var userIdObj) || userIdObj is not int userId)
            return false;

        var user = await _db.Usuarios.FindAsync(userId);
        if (user == null)
            return false;

        return user.Role == UserRole.Admin.ToString()
            || user.Role == UserRole.Supervisor.ToString()
            || user.Role == "Manager";
    }
}

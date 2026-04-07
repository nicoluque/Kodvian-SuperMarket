using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/totem-shifts")]
[OperatorSessionAuth]
public class TotemShiftController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TotemShiftController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("transitions")]
    public async Task<ActionResult<List<object>>> GetTransitions([FromQuery] int? storeId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!await IsAdminOrSupervisorAsync()) return Forbid();

        var query = _db.Sales
            .Include(s => s.Payments)
            .Where(s => s.ShiftAssignmentStatus == "Transition")
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(s => s.StoreId == storeId.Value);
        if (from.HasValue)
            query = query.Where(s => s.CreatedAt >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
        if (to.HasValue)
            query = query.Where(s => s.CreatedAt <= DateTime.SpecifyKind(to.Value, DateTimeKind.Utc));

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(500)
            .Select(s => new
            {
                id = s.Id,
                storeId = s.StoreId,
                createdAt = s.CreatedAt,
                total = s.Total,
                status = s.Status,
                shiftBucket = s.ShiftBucket,
                expectedShiftBucket = s.ExpectedShiftBucket,
                shiftAssignmentStatus = s.ShiftAssignmentStatus,
                lateShiftOpen = s.LateShiftOpen,
                paymentMethods = s.Payments.Select(p => p.PaymentMethod).ToList()
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("sales/{saleId}/assign")]
    public async Task<ActionResult<object>> ReassignTransitionSale(int saleId, [FromBody] TotemTransitionReassignRequest request)
    {
        if (!await IsAdminOrSupervisorAsync()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "Debes ingresar un motivo" });

        var userId = ResolveSessionUserId();
        if (!userId.HasValue) return Unauthorized();

        var sale = await _db.Sales.FindAsync(saleId);
        if (sale == null) return NotFound();

        sale.ShiftAssignmentStatus = "Assigned";
        sale.ShiftBucket = request.ShiftBucket;
        sale.ExpectedShiftBucket = request.ShiftBucket;
        sale.ShiftAssignedAt = DateTime.UtcNow;
        sale.ShiftAssignedByUsuarioId = userId;
        sale.ShiftAssignmentReason = request.Reason.Trim();

        _db.AuditEvents.Add(new AuditEvent
        {
            UsuarioId = userId,
            StoreId = sale.StoreId,
            EventType = AuditEventType.Other.ToString(),
            Description = $"Totem transition reassigned sale {sale.Id}",
            AdditionalData = $"ShiftBucket:{request.ShiftBucket};Reason:{request.Reason}",
            CreatedAt = DateTime.UtcNow,
            Success = true
        });

        await _db.SaveChangesAsync();
        return Ok(new
        {
            saleId = sale.Id,
            shiftAssignmentStatus = sale.ShiftAssignmentStatus,
            shiftBucket = sale.ShiftBucket,
            shiftAssignedAt = sale.ShiftAssignedAt,
            lateShiftOpen = sale.LateShiftOpen
        });
    }

    [HttpGet("sales/{saleId}/history")]
    public async Task<ActionResult<List<object>>> GetShiftHistory(int saleId)
    {
        if (!await IsAdminOrSupervisorAsync()) return Forbid();

        var history = await _db.AuditEvents
            .Where(a => a.Description != null && a.Description.Contains($"sale {saleId}"))
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => (object)new
            {
                id = a.Id,
                createdAt = a.CreatedAt,
                usuarioId = a.UsuarioId,
                description = a.Description,
                additionalData = a.AdditionalData
            })
            .ToListAsync();

        return Ok(history);
    }

    private int? ResolveSessionUserId()
    {
        return HttpContext.Items.TryGetValue("SessionUsuarioId", out var userObj) && userObj is int uid ? uid : null;
    }

    private async Task<bool> IsAdminOrSupervisorAsync()
    {
        var userId = ResolveSessionUserId();
        if (!userId.HasValue) return false;

        var usuario = await _db.Usuarios.FindAsync(userId.Value);
        if (usuario == null) return false;

        return usuario.Role == UserRole.Admin.ToString() || usuario.Role == UserRole.Supervisor.ToString();
    }
}

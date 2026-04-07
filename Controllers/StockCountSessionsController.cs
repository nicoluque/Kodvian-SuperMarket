using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/stock-count-sessions")]
[DeviceAuth]
[OperatorSessionAuth]
public class StockCountSessionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StockCountSessionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<StockCountSessionSummaryDto>>> List()
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var sessions = await _db.StockCountSessions
            .Include(s => s.Lines)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StockCountSessionSummaryDto
            {
                Id = s.Id,
                SessionType = s.SessionType,
                Status = s.Status,
                TotalLines = s.Lines.Count,
                ErrorLines = s.Lines.Count(l => l.Error != null && l.Error != ""),
                CreatedAt = s.CreatedAt,
                CommittedAt = s.CommittedAt
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StockOpeningPreviewResponse>> GetById(int id)
    {
        if (!await IsAdminOrManagerAsync()) return Forbid();

        var session = await _db.StockCountSessions.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        var lines = session.Lines.OrderBy(l => l.RowNumber).Select(l => new StockOpeningPreviewLineDto
        {
            Id = l.Id,
            RowNumber = l.RowNumber,
            ProductId = l.ProductId,
            Barcode = l.Barcode,
            QuickCode = l.QuickCode,
            ProductName = l.ProductName,
            CurrentVendibleQty = l.CurrentVendibleQty,
            CurrentReclamoQty = l.CurrentReclamoQty,
            CurrentMermaQty = l.CurrentMermaQty,
            TargetVendibleQty = l.TargetVendibleQty,
            TargetReclamoQty = l.TargetReclamoQty,
            TargetMermaQty = l.TargetMermaQty,
            DeltaVendibleQty = l.DeltaVendibleQty,
            DeltaReclamoQty = l.DeltaReclamoQty,
            DeltaMermaQty = l.DeltaMermaQty,
            Error = l.Error
        }).ToList();

        return Ok(new StockOpeningPreviewResponse
        {
            SessionId = session.Id,
            SessionType = session.SessionType,
            Status = session.Status,
            TotalRows = lines.Count,
            ErrorRows = lines.Count(l => !string.IsNullOrWhiteSpace(l.Error)),
            RequiresExplicitConfirmation = !string.IsNullOrWhiteSpace(session.WarningMessage),
            WarningMessage = session.WarningMessage,
            Lines = lines
        });
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

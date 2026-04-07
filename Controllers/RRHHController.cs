using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/rrhh")]
[DeviceAuth]
[OperatorSessionAuth]
public class RRHHController : ControllerBase
{
    private readonly IRRHHService _rrhhService;
    private readonly ApplicationDbContext _context;

    public RRHHController(IRRHHService rrhhService, ApplicationDbContext context)
    {
        _rrhhService = rrhhService;
        _context = context;
    }

    [HttpPost("punch")]
    public async Task<ActionResult<TimePunchResponseDto>> Punch()
    {
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;

        var openCashSession = await _context.CashSessions
            .Where(cs => cs.DeviceId == deviceId && cs.Status == CashSessionStatus.Open.ToString())
            .OrderByDescending(cs => cs.OpenedAt)
            .FirstOrDefaultAsync();

        var punch = await _rrhhService.PunchAsync(usuarioId, deviceId, openCashSession?.Id, operatorSessionId);
        var usuario = await _context.Usuarios.FindAsync(usuarioId);

        return Ok(new TimePunchResponseDto
        {
            Id = punch.Id,
            UsuarioId = punch.UsuarioId,
            UsuarioName = usuario?.Username,
            PunchType = punch.PunchType,
            PunchTime = punch.PunchTime,
            IsAdjusted = punch.IsAdjusted
        });
    }

    [HttpGet("punches")]
    public async Task<ActionResult<List<TimePunchDto>>> GetPunches([FromQuery] int? usuarioId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        if (!IsManagerOrAdmin(currentUser))
            usuarioId = currentUser.Id;

        var punches = await _rrhhService.GetPunchesAsync(usuarioId, from, to);
        return Ok(punches);
    }

    [HttpGet("inconsistencies")]
    public async Task<ActionResult<List<TimePunchInconsistencyDto>>> GetInconsistencies([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();
        if (!IsManagerOrAdmin(currentUser))
            return Forbid();

        var inconsistencies = await _rrhhService.GetInconsistenciesAsync(from, to);
        return Ok(inconsistencies);
    }

    [HttpPost("punch-adjustments")]
    public async Task<ActionResult<TimePunchDto>> AdjustPunch([FromBody] CreateTimePunchAdjustmentDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();
        if (!IsManagerOrAdmin(currentUser))
            return Forbid();

        try
        {
            var punch = await _rrhhService.AdjustPunchAsync(request.TimePunchId, currentUser.Id, request.NewPunchTime, request.Reason);
            var usuario = await _context.Usuarios.FindAsync(punch.UsuarioId);
            return Ok(new TimePunchDto
            {
                Id = punch.Id,
                UsuarioId = punch.UsuarioId,
                UsuarioName = usuario?.Username,
                CashSessionId = punch.CashSessionId,
                DeviceId = punch.DeviceId,
                PunchType = punch.PunchType,
                PunchTime = punch.PunchTime,
                IsAdjusted = punch.IsAdjusted,
                AdjustedAt = punch.AdjustedAt,
                AdjustedById = punch.AdjustedById,
                AdjustedByName = currentUser.Username,
                CreatedAt = punch.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("extras")]
    public async Task<ActionResult<EmployeeExtraDto>> CreateExtra([FromBody] CreateEmployeeExtraDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();
        if (!IsManagerOrAdmin(currentUser))
            return Forbid();

        var extra = await _rrhhService.CreateExtraAsync(request.UsuarioId, currentUser.Id, request.ExtraDate, request.Hours, request.Reason);
        return Ok(new EmployeeExtraDto
        {
            Id = extra.Id,
            UsuarioId = extra.UsuarioId,
            CreatedById = extra.CreatedById,
            ExtraDate = extra.ExtraDate,
            Year = extra.Year,
            Month = extra.Month,
            Hours = extra.Hours,
            Reason = extra.Reason,
            IsApproved = extra.IsApproved,
            CreatedAt = extra.CreatedAt
        });
    }

    [HttpGet("extras")]
    public async Task<ActionResult<List<EmployeeExtraDto>>> GetExtras([FromQuery] int? usuarioId = null, [FromQuery] int? year = null, [FromQuery] int? month = null, [FromQuery] bool? approved = null)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        if (!IsManagerOrAdmin(currentUser))
            usuarioId = currentUser.Id;

        var extras = await _rrhhService.GetExtrasAsync(usuarioId, year, month, approved);
        return Ok(extras);
    }

    [HttpPost("payroll-receipts")]
    public async Task<ActionResult<PayrollReceiptDto>> UploadReceipt([FromBody] UploadPayrollReceiptDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();
        if (!IsManagerOrAdmin(currentUser))
            return Forbid();

        byte[] fileContent;
        try
        {
            fileContent = Convert.FromBase64String(request.FileContentBase64);
        }
        catch
        {
            return BadRequest(new { message = "Invalid base64 file content" });
        }

        if (fileContent.Length == 0)
            return BadRequest(new { message = "Empty file" });

        var receipt = await _rrhhService.UploadReceiptAsync(request.UsuarioId, request.Year, request.Month, request.FileName, fileContent);
        var usuario = await _context.Usuarios.FindAsync(receipt.UsuarioId);
        return Ok(new PayrollReceiptDto
        {
            Id = receipt.Id,
            UsuarioId = receipt.UsuarioId,
            UsuarioName = usuario?.Username,
            Year = receipt.Year,
            Month = receipt.Month,
            FileName = receipt.FileName,
            FileSize = receipt.FileSize,
            CreatedAt = receipt.CreatedAt
        });
    }

    [HttpGet("payroll-receipts")]
    public async Task<ActionResult<List<PayrollReceiptDto>>> GetReceipts([FromQuery] int? usuarioId = null, [FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        if (!IsManagerOrAdmin(currentUser))
            usuarioId = currentUser.Id;

        var receipts = await _rrhhService.GetReceiptsAsync(usuarioId, year, month);
        return Ok(receipts);
    }

    [HttpGet("payroll-receipts/{id}/download")]
    public async Task<ActionResult> DownloadReceipt(int id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized();

        var receipt = await _rrhhService.GetReceiptAsync(id);
        if (receipt == null)
            return NotFound();

        if (!IsManagerOrAdmin(currentUser) && receipt.UsuarioId != currentUser.Id)
            return Forbid();

        var fileContent = await _rrhhService.DownloadReceiptAsync(id);
        return File(fileContent, "application/pdf", receipt.FileName);
    }

    private async Task<Usuario?> GetCurrentUserAsync()
    {
        var currentUsuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;
        return await _context.Usuarios.FindAsync(currentUsuarioId);
    }

    private static bool IsManagerOrAdmin(Usuario usuario)
    {
        return usuario.Role == UserRole.Admin.ToString() || usuario.Role == UserRole.Supervisor.ToString();
    }
}

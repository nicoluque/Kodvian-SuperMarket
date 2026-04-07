using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/reports")]
[DeviceAuth]
[OperatorSessionAuth]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;
    private readonly ApplicationDbContext _context;

    public ReportsController(IReportsService reportsService, ApplicationDbContext context)
    {
        _reportsService = reportsService;
        _context = context;
    }

    [HttpGet("sales/daily")]
    public async Task<ActionResult<SalesDailyReportDto>> GetSalesDaily([FromQuery] DateTime date)
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        var report = await _reportsService.GetSalesDailyAsync(date);
        return Ok(report);
    }

    [HttpGet("sales/range")]
    public async Task<ActionResult<List<SalesRangeDayReportDto>>> GetSalesRange([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        if (from > to)
            return BadRequest(new { message = "Invalid date range: from must be less than or equal to to" });

        var report = await _reportsService.GetSalesRangeAsync(from, to);
        return Ok(report);
    }

    [HttpGet("transfers/pending")]
    public async Task<ActionResult<List<PendingTransferReportDto>>> GetPendingTransfers([FromQuery] int? olderThanHours = null, [FromQuery] int? customerId = null)
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        var report = await _reportsService.GetPendingTransfersAsync(olderThanHours, customerId);
        return Ok(report);
    }

    [HttpGet("transfers/pending/alerts")]
    public async Task<ActionResult<PendingTransferAlertsDto>> GetPendingTransferAlerts([FromQuery] int olderThanHours = 24)
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        var report = await _reportsService.GetPendingTransferAlertsAsync(olderThanHours);
        return Ok(report);
    }

    [HttpGet("stock/summary")]
    public async Task<ActionResult<List<StockBalanceDto>>> GetStockSummary()
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        var report = await _reportsService.GetStockSummaryAsync();
        return Ok(report);
    }

    [HttpGet("stock/cigarettes")]
    public async Task<ActionResult<List<StockBalanceDto>>> GetStockCigarettes()
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        var report = await _reportsService.GetStockCigarettesAsync();
        return Ok(report);
    }

    [HttpGet("suppliers/claims")]
    public async Task<ActionResult<List<SupplierClaimDto>>> GetSupplierClaims([FromQuery] string? status = null)
    {
        if (!await IsAdminOrManager())
            return Forbid();

        var report = await _reportsService.GetSupplierClaimsAsync(status);
        return Ok(report);
    }

    [HttpGet("suppliers/claims/overdue")]
    public async Task<ActionResult<List<SupplierClaimDto>>> GetSupplierClaimsOverdue([FromQuery] int days = 7)
    {
        if (!await IsAdminOrManager())
            return Forbid();

        var report = await _reportsService.GetSupplierClaimsOverdueAsync(days);
        return Ok(report);
    }

    [HttpGet("suppliers/credits")]
    public async Task<ActionResult<List<SupplierCreditBySupplierDto>>> GetSupplierCreditsBySupplier()
    {
        if (!await IsAdminOrManager())
            return Forbid();

        var report = await _reportsService.GetSupplierCreditsBySupplierAsync();
        return Ok(report);
    }

    [HttpGet("containers/debtors")]
    public async Task<ActionResult<List<ContainerDebtorDto>>> GetContainerDebtors([FromQuery] int top = 10)
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        var report = await _reportsService.GetContainerDebtorsAsync(top);
        return Ok(report);
    }

    [HttpGet("containers/by-customer")]
    public async Task<ActionResult<List<ContainerByCustomerDto>>> GetContainersByCustomer([FromQuery] int customerId)
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        var report = await _reportsService.GetContainersByCustomerAsync(customerId);
        return Ok(report);
    }

    [HttpGet("cigarettes/counts")]
    public async Task<ActionResult<List<CigaretteCountReportDto>>> GetCigaretteCounts([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!await IsCashierOrAbove())
            return Forbid();

        if (from.HasValue && to.HasValue && from > to)
            return BadRequest(new { message = "Invalid date range: from must be less than or equal to to" });

        var report = await _reportsService.GetCigaretteCountsAsync(from, to);
        return Ok(report);
    }

    [HttpGet("rrhh/hours")]
    public async Task<ActionResult<List<RrhhHoursDto>>> GetRrhhHours([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!await IsAdminOrManager())
            return Forbid();

        if (from > to)
            return BadRequest(new { message = "Invalid date range: from must be less than or equal to to" });

        var report = await _reportsService.GetRrhhHoursAsync(from, to);
        return Ok(report);
    }

    [HttpGet("rrhh/inconsistencies")]
    public async Task<ActionResult<List<TimePunchInconsistencyDto>>> GetRrhhInconsistencies([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        if (!await IsAdminOrManager())
            return Forbid();

        if (from.HasValue && to.HasValue && from > to)
            return BadRequest(new { message = "Invalid date range: from must be less than or equal to to" });

        var report = await _reportsService.GetRrhhInconsistenciesAsync(from, to);
        return Ok(report);
    }

    private async Task<Usuario?> GetCurrentUserAsync()
    {
        if (!HttpContext.Items.TryGetValue("SessionUsuarioId", out var sessionUserIdObj) || sessionUserIdObj is not int sessionUserId)
            return null;

        return await _context.Usuarios.FindAsync(sessionUserId);
    }

    private async Task<bool> IsAdminOrManager()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return false;

        return currentUser.Role == UserRole.Admin.ToString() || currentUser.Role == UserRole.Supervisor.ToString();
    }

    private async Task<bool> IsCashierOrAbove()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return false;

        return currentUser.Role == UserRole.Operator.ToString()
            || currentUser.Role == UserRole.Supervisor.ToString()
            || currentUser.Role == UserRole.Admin.ToString();
    }
}

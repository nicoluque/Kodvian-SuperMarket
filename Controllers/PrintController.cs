using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/print")]
public class PrintController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public PrintController(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("sales/{id}")]
    public async Task<ActionResult<SalePrintDto>> Sale(int id, [FromQuery] bool reprint = false)
    {
        var sale = await _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();
        var branding = await ResolveBrandingAsync(sale.StoreId);
        if (reprint)
            await LogReprintAsync($"sale:{id}", sale.StoreId);

        return Ok(new SalePrintDto
        {
            SaleId = sale.Id,
            Status = sale.Status,
            InvoiceNumber = sale.InvoiceNumber,
            CreatedAt = sale.CreatedAt,
            CompletedAt = sale.CompletedAt,
            CustomerName = sale.Customer?.FullName,
            Subtotal = sale.Subtotal,
            Discount = sale.Discount,
            Tax = sale.Tax,
            Total = sale.Total,
            Items = sale.Items.Select(i => new SalePrintLineDto
            {
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal
            }).ToList(),
            Payments = sale.Payments.Select(p => new SalePrintPaymentDto
            {
                Method = p.PaymentMethod,
                Status = p.Status,
                Amount = p.Amount,
                Reference = p.Reference
            }).ToList(),
            Branding = branding
        });
    }

    [HttpGet("customer-payments/{id}")]
    public async Task<ActionResult<CustomerPaymentPrintDto>> CustomerPayment(int id, [FromQuery] bool reprint = false)
    {
        var movement = await _db.CustomerAccountMovements
            .Include(m => m.Customer)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movement == null) return NotFound();
        var branding = await ResolveBrandingAsync(GetHeaderStoreId());
        if (reprint)
            await LogReprintAsync($"customer-payment:{id}", GetHeaderStoreId());

        return Ok(new CustomerPaymentPrintDto
        {
            PaymentId = movement.Id,
            CustomerId = movement.CustomerId,
            CustomerName = movement.Customer.FullName,
            Amount = movement.Amount,
            MovementType = movement.MovementType,
            Reference = movement.ReferenceType,
            Description = movement.Description,
            CreatedAt = movement.CreatedAt,
            Branding = branding
        });
    }

    [HttpGet("returns/{id}")]
    public async Task<ActionResult<ReturnPrintDto>> Return(int id, [FromQuery] bool reprint = false)
    {
        var ret = await _db.SaleReturns
            .Include(r => r.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (ret == null) return NotFound();
        var branding = await ResolveBrandingAsync(ret.StoreId);
        if (reprint)
            await LogReprintAsync($"return:{id}", ret.StoreId);

        return Ok(new ReturnPrintDto
        {
            ReturnId = ret.Id,
            OriginalSaleId = ret.OriginalSaleId,
            RefundPreference = ret.RefundPreference,
            RefundTotal = ret.RefundTotal,
            ReturnedSubtotal = ret.ReturnedSubtotal,
            ReturnedCigaretteSurchargeShare = ret.ReturnedCigaretteSurchargeShare,
            CustomerAlias = ret.CustomerAlias,
            CreatedAt = ret.CreatedAt,
            Lines = ret.Lines.Select(l => new ReturnPrintLineDto
            {
                ProductName = l.Product?.Name ?? $"Producto {l.ProductId}",
                QtyReturned = l.QtyReturned,
                Condition = l.Condition,
                LineRefundAmount = l.LineRefundAmount
            }).ToList(),
            Branding = branding
        });
    }

    [HttpGet("cash-movements/{id}")]
    public async Task<ActionResult<CashMovementPrintDto>> CashMovement(int id, [FromQuery] bool reprint = false)
    {
        var movement = await _db.CashSessionMoneyMovements.FirstOrDefaultAsync(m => m.Id == id);
        if (movement == null) return NotFound();

        var branding = await ResolveBrandingAsync(movement.StoreId);
        if (reprint)
            await LogReprintAsync($"cash-movement:{id}", movement.StoreId);

        return Ok(new CashMovementPrintDto
        {
            MovementId = movement.Id,
            CashSessionId = movement.CashSessionId,
            Method = movement.Method,
            Type = movement.Type,
            SignedAmount = movement.SignedAmount,
            Reason = movement.Reason,
            Category = movement.Category,
            CreatedAt = movement.CreatedAt,
            Branding = branding
        });
    }

    [HttpGet("cash-sessions/{id}/close-summary")]
    public async Task<ActionResult<CashSessionClosePrintDto>> CashClose(int id, [FromQuery] bool reprint = false)
    {
        var session = await _db.CashSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session == null) return NotFound();

        var movements = await _db.CashSessionMoneyMovements
            .Where(m => m.CashSessionId == id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var branding = await ResolveBrandingAsync(session.StoreId);
        if (reprint)
            await LogReprintAsync($"cash-close:{id}", session.StoreId);

        return Ok(new CashSessionClosePrintDto
        {
            CashSessionId = session.Id,
            Shift = session.Shift,
            OpenedAt = session.OpenedAt,
            ClosedAt = session.ClosedAt,
            OpeningCash = session.OpeningCash,
            TotalCash = session.TotalCash,
            TotalCard = session.TotalCard,
            TotalTransfer = session.TotalTransfer,
            TotalCredit = session.TotalCredit,
            DeclaredCash = session.DeclaredCash,
            DeclaredCard = session.DeclaredCard,
            DeclaredTransfer = session.DeclaredTransfer,
            DeclaredCredit = session.DeclaredCredit,
            DiffTotal = session.DiffTotal,
            CloseNotes = session.CloseNotes,
            Movements = movements.Select(m => new CashMovementPrintDto
            {
                MovementId = m.Id,
                CashSessionId = m.CashSessionId,
                Method = m.Method,
                Type = m.Type,
                SignedAmount = m.SignedAmount,
                Reason = m.Reason,
                Category = m.Category,
                CreatedAt = m.CreatedAt,
                Branding = branding
            }).ToList(),
            Branding = branding
        });
    }

    private async Task<PrintBrandingDto> ResolveBrandingAsync(int? storeId)
    {
        int? tenantId = null;
        if (storeId.HasValue)
            tenantId = await _db.Stores.Where(s => s.Id == storeId.Value).Select(s => (int?)s.TenantId).FirstOrDefaultAsync();

        if (!tenantId.HasValue)
        {
            var headerStoreId = GetHeaderStoreId();
            if (headerStoreId.HasValue)
                tenantId = await _db.Stores.Where(s => s.Id == headerStoreId.Value).Select(s => (int?)s.TenantId).FirstOrDefaultAsync();
        }

        if (!tenantId.HasValue)
            tenantId = await _db.Tenants.OrderBy(t => t.Id).Select(t => (int?)t.Id).FirstOrDefaultAsync();

        if (!tenantId.HasValue)
            return new PrintBrandingDto();

        var branding = await _db.TenantBrandingSettings.FirstOrDefaultAsync(b => b.TenantId == tenantId.Value);
        if (branding == null)
            return new PrintBrandingDto();

        return new PrintBrandingDto
        {
            DisplayName = string.IsNullOrWhiteSpace(branding.DisplayName) ? "Kodvian SuperMarket" : branding.DisplayName,
            LogoUrl = branding.LogoUrl,
            TicketHeaderText = string.IsNullOrWhiteSpace(branding.TicketHeaderText) ? "Gracias por su compra" : branding.TicketHeaderText,
            TicketFooterText = string.IsNullOrWhiteSpace(branding.TicketFooterText) ? "Conserve su comprobante" : branding.TicketFooterText,
            ReturnPolicyText = string.IsNullOrWhiteSpace(branding.ReturnPolicyText) ? "Cambios dentro de 24h con ticket" : branding.ReturnPolicyText
        };
    }

    private int? GetHeaderStoreId()
    {
        var raw = Request.Headers["X-Store-Id"].FirstOrDefault();
        return int.TryParse(raw, out var id) ? id : null;
    }

    private async Task LogReprintAsync(string receiptRef, int? storeId)
    {
        var userId = HttpContext.Items.TryGetValue("SessionUsuarioId", out var uid) && uid is int u ? u : (int?)null;
        await _audit.LogAsync(
            AuditEventType.RECEIPT_REPRINTED,
            userId,
            "Receipt printed/reprinted",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            "Print",
            receiptRef,
            true,
            storeId);
    }
}

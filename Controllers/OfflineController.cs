using Microsoft.AspNetCore.Mvc;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Filters;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/offline")]
[DeviceAuth]
[OperatorSessionAuth]
public class OfflineController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly ICashSessionService _cashSessionService;

    public OfflineController(ISaleService saleService, ICashSessionService cashSessionService)
    {
        _saleService = saleService;
        _cashSessionService = cashSessionService;
    }

    [HttpPost("manual-sales")]
    public async Task<ActionResult<SaleResponse>> ImportManualSale([FromBody] ManualSaleImportRequest request)
    {
        var deviceId = (int)HttpContext.Items["DeviceId"]!;
        var operatorSessionId = (int)HttpContext.Items["SessionId"]!;
        var usuarioId = (int)HttpContext.Items["SessionUsuarioId"]!;

        var db = HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var device = await db.Devices.FindAsync(deviceId);
        if (device?.DeviceType != "CashRegister")
            return BadRequest(new { message = "Solo los dispositivos de caja pueden importar ventas manuales" });

        var cashSession = await _cashSessionService.GetCurrentForDeviceAsync(deviceId);
        if (cashSession == null)
            return BadRequest(new { message = "No hay una caja abierta" });

        try
        {
            var items = request.Items.Select(i => (i.Code, i.Quantity, i.UnitPrice)).ToList();
            var sale = await _saleService.ImportManualSaleAsync(
                cashSession.Id,
                operatorSessionId,
                usuarioId,
                request.ExternalTicketId,
                request.OriginalCreatedAt,
                request.CustomerAlias,
                items
            );

            await _cashSessionService.RecalculateTotalsAsync(cashSession.Id);

            return Ok(new SaleResponse
            {
                Id = sale.Id,
                CartId = sale.CartId,
                CustomerId = sale.CustomerId,
                DeviceId = sale.DeviceId,
                Status = sale.Status,
                Subtotal = sale.Subtotal,
                Discount = sale.Discount,
                Tax = sale.Tax,
                Total = sale.Total,
                InvoiceNumber = sale.InvoiceNumber,
                CreatedAt = sale.CreatedAt,
                CompletedAt = sale.CompletedAt,
                Items = sale.Items.Select(i => new SaleItemResponse
                {
                    Id = i.Id,
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Discount = i.Discount,
                    Subtotal = i.Subtotal
                }).ToList(),
                Payments = sale.Payments.Select(p => new SalePaymentResponse
                {
                    Id = p.Id,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    Provider = p.Provider,
                    ExternalReference = p.ExternalReference,
                    ConfirmedAt = p.ConfirmedAt,
                    ConfirmNotes = p.ConfirmNotes,
                    CreatedAt = p.CreatedAt
                }).ToList()
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

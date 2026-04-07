using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Controllers;

[ApiController]
[Route("api/v1/payments/mercadopago")]
public class MercadoPagoWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;
    private readonly ISaleService _saleService;
    private readonly ICashSessionService _cashSessionService;

    public MercadoPagoWebhookController(ApplicationDbContext db, IConfiguration config, IAuditService audit, ISaleService saleService, ICashSessionService cashSessionService)
    {
        _db = db;
        _config = config;
        _audit = audit;
        _saleService = saleService;
        _cashSessionService = cashSessionService;
    }

    [HttpPost("webhook")]
    public async Task<ActionResult<object>> Webhook([FromBody] JsonElement payload)
    {
        if (!bool.TryParse(_config["MercadoPago:MercadoPagoEnabled"], out var enabled) || !enabled)
            return Ok(new { ignored = true, reason = "MercadoPago disabled" });

        if (!IsValidSecret())
            return Unauthorized(new { message = "Invalid MercadoPago webhook signature" });

        var eventId = TryRead(payload, "id")
            ?? TryRead(payload, "event_id")
            ?? TryRead(payload, "data.id")
            ?? Guid.NewGuid().ToString("N");

        var eventType = TryRead(payload, "type") ?? TryRead(payload, "action") ?? "unknown";
        var paymentId = TryRead(payload, "payment_id") ?? TryRead(payload, "data.id");
        var externalReference = TryRead(payload, "external_reference") ?? TryRead(payload, "data.external_reference");
        var status = (TryRead(payload, "status") ?? TryRead(payload, "data.status") ?? "unknown").ToLowerInvariant();

        var providerEvent = new PaymentProviderEvent
        {
            Provider = "MercadoPago",
            EventId = eventId,
            EventType = eventType,
            ExternalReference = externalReference,
            PayloadJson = payload.GetRawText(),
            Status = "Received",
            ReceivedAt = DateTime.UtcNow
        };

        try
        {
            _db.PaymentProviderEvents.Add(providerEvent);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Ok(new { duplicate = true, eventId });
        }

        await _audit.LogAsync(
            AuditEventType.MP_WEBHOOK_RECEIVED,
            null,
            "MercadoPago webhook received",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            "Webhook",
            $"eventId={eventId}; type={eventType}; paymentId={paymentId}; extRef={externalReference}; status={status}",
            true);

        try
        {
            if (!int.TryParse(externalReference, out var saleId) || saleId <= 0)
            {
                providerEvent.Status = "Ignored";
                providerEvent.ErrorMessage = "external_reference missing or invalid";
                providerEvent.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new { ignored = true, reason = "invalid external_reference" });
            }

            var sale = await _db.Sales
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                providerEvent.Status = "Error";
                providerEvent.ErrorMessage = "sale not found";
                providerEvent.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return NotFound(new { message = "Sale not found for external_reference" });
            }

            var qrPayment = sale.Payments
                .FirstOrDefault(p => p.PaymentMethod == PaymentMethod.QrMp.ToString() && p.Status == PaymentStatus.Pending.ToString())
                ?? sale.Payments.FirstOrDefault(p => p.PaymentMethod == PaymentMethod.QrMp.ToString());

            if (qrPayment == null)
            {
                providerEvent.Status = "Ignored";
                providerEvent.ErrorMessage = "QrMp payment not found";
                providerEvent.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new { ignored = true, reason = "QrMp payment not found" });
            }

            qrPayment.Provider = "MercadoPago";
            qrPayment.ExternalReference = externalReference;

            if (status is "approved" or "accredited")
            {
                qrPayment.Status = PaymentStatus.Confirmed.ToString();
                qrPayment.Reference = paymentId ?? qrPayment.Reference;
                qrPayment.ConfirmedAt = DateTime.UtcNow;

                var confirmedTotal = sale.Payments
                    .Where(p => p.Status == PaymentStatus.Confirmed.ToString())
                    .Sum(p => p.Amount);

                if (confirmedTotal >= sale.Total)
                {
                    sale.Status = SaleStatus.Paid.ToString();
                    if (string.IsNullOrWhiteSpace(sale.InvoiceNumber))
                        sale.InvoiceNumber = await _saleService.GenerateInvoiceNumber();
                    sale.CompletedAt = DateTime.UtcNow;
                }

                providerEvent.Status = "Processed";
                providerEvent.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                if (sale.CashSessionId.HasValue)
                    await _cashSessionService.RecalculateTotalsAsync(sale.CashSessionId.Value);

                await _audit.LogAsync(
                    AuditEventType.MP_PAYMENT_CONFIRMED,
                    sale.OperatorSession?.UsuarioId,
                    $"MercadoPago payment confirmed for sale {sale.Id}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    "Webhook",
                    $"paymentId={paymentId}; saleId={sale.Id}; totalConfirmed={confirmedTotal}",
                    true,
                    sale.StoreId);

                return Ok(new { ok = true, eventId, saleId = sale.Id, paymentStatus = qrPayment.Status, saleStatus = sale.Status });
            }

            if (status is "rejected" or "cancelled" or "cancelled_by_user")
            {
                qrPayment.Status = PaymentStatus.Rejected.ToString();
                qrPayment.Reference = paymentId ?? qrPayment.Reference;
                qrPayment.ConfirmNotes = "MercadoPago rejected/cancelled";

                providerEvent.Status = "Processed";
                providerEvent.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await _audit.LogAsync(
                    AuditEventType.MP_PAYMENT_REJECTED,
                    sale.OperatorSession?.UsuarioId,
                    $"MercadoPago payment rejected for sale {sale.Id}",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    "Webhook",
                    $"paymentId={paymentId}; saleId={sale.Id}; status={status}",
                    true,
                    sale.StoreId);

                return Ok(new { ok = true, eventId, saleId = sale.Id, paymentStatus = qrPayment.Status, saleStatus = sale.Status });
            }

            providerEvent.Status = "Ignored";
            providerEvent.ErrorMessage = $"Unhandled status: {status}";
            providerEvent.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { ignored = true, status });
        }
        catch (Exception ex)
        {
            providerEvent.Status = "Error";
            providerEvent.ErrorMessage = ex.Message;
            providerEvent.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw;
        }
    }

    private bool IsValidSecret()
    {
        var expected = _config["MercadoPago:MercadoPagoWebhookSecret"];
        if (string.IsNullOrWhiteSpace(expected))
            return true;

        var fromSig = Request.Headers["X-MP-Signature"].FirstOrDefault();
        var fromSecret = Request.Headers["X-Webhook-Secret"].FirstOrDefault();
        var fromAuth = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        return string.Equals(expected, fromSig, StringComparison.Ordinal)
            || string.Equals(expected, fromSecret, StringComparison.Ordinal)
            || string.Equals(expected, fromAuth, StringComparison.Ordinal);
    }

    private static string? TryRead(JsonElement root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
                return null;
            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace KodvianSuperMarket.Middleware;

public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var (status, code, message, details) = MapException(ex);

            _logger.LogError(ex,
                "API exception {Code} traceId={TraceId} tenantId={TenantId} storeId={StoreId} deviceId={DeviceId} userId={UserId} operatorId={OperatorId}",
                code,
                traceId,
                context.Items.TryGetValue("TenantId", out var tenantId) ? tenantId : null,
                context.Items.TryGetValue("StoreId", out var storeId) ? storeId : null,
                context.Items.TryGetValue("DeviceId", out var deviceId) ? deviceId : null,
                context.Items.TryGetValue("SessionUsuarioId", out var userId) ? userId : null,
                context.Items.TryGetValue("SessionId", out var operatorId) ? operatorId : null
            );

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                code,
                message,
                details,
                traceId
            });

            await context.Response.WriteAsync(payload);
        }
    }

    private static (int status, string code, string message, object? details) MapException(Exception ex)
    {
        if (ex is ValidationException vex)
            return (400, "VALIDATION_ERROR", "Error de validación", vex.Message);

        if (ex is DbUpdateException dbex)
        {
            var details = dbex.InnerException?.Message ?? dbex.Message;
            var lower = details.ToLowerInvariant();

            if (lower.Contains("ix_productstocks_productid_bucket") && lower.Contains("duplicate key"))
                return (400, "VALIDATION_ERROR", "Ya existe un registro de stock para ese producto y bucket.", details);

            if (lower.Contains("duplicate key"))
                return (400, "VALIDATION_ERROR", "Ya existe un registro con esos datos.", details);

            return (400, "VALIDATION_ERROR", "No se pudo guardar por una validación de base de datos.", details);
        }

        if (ex is UnauthorizedAccessException)
            return (401, "UNAUTHORIZED", "Unauthorized", null);

        if (ex is InvalidOperationException ioex)
        {
            var msg = TranslateKnownMessage(ioex.Message);
            var lower = msg.ToLowerInvariant();

            if (lower.Contains("duplicate submission"))
                return (409, "DUPLICATE_SUBMISSION", msg, null);

            if (lower.Contains("cash session") && lower.Contains("open"))
                return (409, "CASH_SESSION_NOT_OPEN", msg, null);
            if (lower.Contains("missingrequiredtasks") || lower.Contains("required tasks are pending") || (lower.Contains("close") && lower.Contains("blocked")))
                return (409, "SHIFT_CLOSE_BLOCKED", msg, null);
            if (lower.Contains("pending transfer") && lower.Contains("already"))
                return (409, "PENDING_TRANSFER_LIMIT_REACHED", msg, null);
            if (lower.Contains("return window") || lower.Contains("24h") || lower.Contains("window exceeded"))
                return (409, "RETURN_WINDOW_EXPIRED", msg, null);

            return (400, "BUSINESS_RULE_ERROR", msg, null);
        }

        return (500, "INTERNAL_ERROR", "Error interno del servidor", ex.Message);
    }

    private static string TranslateKnownMessage(string message)
    {
        var normalized = message.Trim();
        var lower = normalized.ToLowerInvariant();

        if (lower.Contains("cart not found")) return "Carrito no encontrado";
        if (lower.Contains("cart is not open")) return "El carrito no está abierto";
        if (lower.Contains("cart is empty")) return "El carrito está vacío";
        if (lower.Contains("cart must be sent to cashier first")) return "Primero debes enviar el carrito a caja";
        if (lower.Contains("cash session not found")) return "No se encontró la sesión de caja";
        if (lower.Contains("cash session is not open")) return "La sesión de caja no está abierta";
        if (lower.Contains("cash session must be open")) return "La sesión de caja debe estar abierta";
        if (lower.Contains("payment total is less than sale total")) return "El total de pagos es menor al total de la venta";
        if (lower.Contains("sale not found")) return "Venta no encontrada";
        if (lower.Contains("session not found")) return "Sesión no encontrada";
        if (lower.Contains("reason is required")) return "Debes ingresar un motivo";

        return normalized;
    }
}

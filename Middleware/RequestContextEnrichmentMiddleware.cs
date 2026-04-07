using System.Diagnostics;
using Serilog.Context;

namespace KodvianSuperMarket.Middleware;

public class RequestContextEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var tenantId = context.Items.TryGetValue("TenantId", out var tid) ? tid : null;
        var storeId = context.Items.TryGetValue("StoreId", out var sid) ? sid : null;
        if (storeId == null && context.Request.Headers.TryGetValue("X-Store-Id", out var storeHeader))
            storeId = storeHeader.ToString();
        var deviceId = context.Items.TryGetValue("DeviceId", out var did) ? did : null;
        var userId = context.Items.TryGetValue("SessionUsuarioId", out var uid) ? uid : null;
        var operatorId = context.Items.TryGetValue("SessionId", out var oid) ? oid : null;

        using (LogContext.PushProperty("traceId", traceId))
        using (LogContext.PushProperty("tenantId", tenantId))
        using (LogContext.PushProperty("storeId", storeId))
        using (LogContext.PushProperty("deviceId", deviceId))
        using (LogContext.PushProperty("userId", userId))
        using (LogContext.PushProperty("operatorId", operatorId))
        {
            await _next(context);
        }
    }
}

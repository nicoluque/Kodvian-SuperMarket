using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.Services;

namespace KodvianSuperMarket.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DeviceAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var deviceService = context.HttpContext.RequestServices.GetRequiredService<IDeviceService>();
        var auditService = context.HttpContext.RequestServices.GetRequiredService<IAuditService>();

        var token = context.HttpContext.Request.Headers["X-Device-Token"].FirstOrDefault();

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var (device, rawToken) = await deviceService.AuthenticateAsync(token);

        if (device == null)
        {
            try
            {
                await auditService.LogAsync(
                    AuditEventType.UnauthorizedAccess,
                    null,
                    "Invalid device token",
                    context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    context.HttpContext.Request.Headers.UserAgent.ToString(),
                    null,
                    null,
                    false
                );
            }
            catch
            {
            }
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["Device"] = device;
        context.HttpContext.Items["DeviceId"] = device.Id;
        context.HttpContext.Items["DeviceUsuarioId"] = device.UsuarioId;
        context.HttpContext.Items["StoreId"] = device.StoreId;

        if (device.StoreId.HasValue)
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var tenantId = await db.Stores
                .Where(s => s.Id == device.StoreId.Value)
                .Select(s => (int?)s.TenantId)
                .FirstOrDefaultAsync();

            if (tenantId.HasValue)
                context.HttpContext.Items["TenantId"] = tenantId.Value;
        }
    }
}

public class OperatorSessionAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var sessionService = context.HttpContext.RequestServices.GetRequiredService<IOperatorSessionService>();
        var auditService = context.HttpContext.RequestServices.GetRequiredService<IAuditService>();

        var token = context.HttpContext.Request.Headers["X-Operator-Session"].FirstOrDefault();

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var (session, rawToken) = await sessionService.AuthenticateAsync(token);

        if (session == null)
        {
            try
            {
                await auditService.LogAsync(
                    AuditEventType.UnauthorizedAccess,
                    null,
                    "Invalid or expired session token",
                    context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    context.HttpContext.Request.Headers.UserAgent.ToString(),
                    null,
                    null,
                    false
                );
            }
            catch
            {
            }
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["Session"] = session;
        context.HttpContext.Items["SessionId"] = session.Id;
        context.HttpContext.Items["SessionUsuarioId"] = session.UsuarioId;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Items["Session"] as OperatorSession;
        
        if (session == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<Data.ApplicationDbContext>();
        var usuario = dbContext.Usuarios.Find(session.UsuarioId);

        if (usuario == null || usuario.Role != "Admin")
        {
            context.Result = new ForbidResult();
        }
    }
}

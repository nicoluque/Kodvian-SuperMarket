using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface ITrainingService
{
    Task<int?> ResolveTrainingTenantIdAsync(int? preferredTenantId = null, int? storeId = null);
    Task EnsureSeededAsync(int tenantId);
    Task<int?> ResetTrainingAsync(int? preferredTenantId = null, int? storeId = null);
}

public class TrainingService : ITrainingService
{
    private const string DemoTenantCode = "demo-tenant";
    private readonly ApplicationDbContext _db;

    public TrainingService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int?> ResolveTrainingTenantIdAsync(int? preferredTenantId = null, int? storeId = null)
    {
        if (preferredTenantId.HasValue)
        {
            var exists = await _db.Tenants.AnyAsync(t => t.Id == preferredTenantId.Value);
            if (exists) return preferredTenantId.Value;
        }

        if (storeId.HasValue)
        {
            var byStore = await _db.Stores.Where(s => s.Id == storeId.Value).Select(s => (int?)s.TenantId).FirstOrDefaultAsync();
            if (byStore.HasValue) return byStore.Value;
        }

        var trainingTenant = await _db.Tenants.Where(t => t.IsTrainingTenant).OrderBy(t => t.Id).Select(t => (int?)t.Id).FirstOrDefaultAsync();
        if (trainingTenant.HasValue) return trainingTenant.Value;

        var demoTenant = await _db.Tenants.Where(t => t.Code == DemoTenantCode).Select(t => (int?)t.Id).FirstOrDefaultAsync();
        if (demoTenant.HasValue) return demoTenant.Value;

        return await _db.Tenants.OrderBy(t => t.Id).Select(t => (int?)t.Id).FirstOrDefaultAsync();
    }

    public async Task EnsureSeededAsync(int tenantId)
    {
        var hasAny = await _db.TrainingChecklists.AnyAsync(c => c.TenantId == tenantId && c.IsActive);
        if (hasAny) return;

        var defaults = GetDefaults();
        var now = DateTime.UtcNow;

        foreach (var def in defaults)
        {
            var checklist = new TrainingChecklist
            {
                TenantId = tenantId,
                Role = def.Role,
                Title = def.Title,
                Description = def.Description,
                IsActive = true,
                CreatedAt = now,
                Items = def.Items.Select((item, idx) => new TrainingChecklistItem
                {
                    Title = item,
                    SortOrder = idx + 1,
                    IsRequired = true,
                    CreatedAt = now
                }).ToList()
            };
            _db.TrainingChecklists.Add(checklist);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<int?> ResetTrainingAsync(int? preferredTenantId = null, int? storeId = null)
    {
        var tenantId = await ResolveTrainingTenantIdAsync(preferredTenantId, storeId);
        if (!tenantId.HasValue) return null;

        var checklistIds = await _db.TrainingChecklists.Where(c => c.TenantId == tenantId.Value).Select(c => c.Id).ToListAsync();
        var runIds = await _db.TrainingChecklistRuns.Where(r => r.TenantId == tenantId.Value).Select(r => r.Id).ToListAsync();

        if (runIds.Count > 0)
            await _db.TrainingChecklistRunItems.Where(i => runIds.Contains(i.TrainingChecklistRunId)).ExecuteDeleteAsync();
        if (checklistIds.Count > 0)
            await _db.TrainingChecklistItems.Where(i => checklistIds.Contains(i.TrainingChecklistId)).ExecuteDeleteAsync();

        await _db.TrainingChecklistRuns.Where(r => r.TenantId == tenantId.Value).ExecuteDeleteAsync();
        await _db.TrainingChecklists.Where(c => c.TenantId == tenantId.Value).ExecuteDeleteAsync();

        await EnsureSeededAsync(tenantId.Value);
        return tenantId.Value;
    }

    private static IReadOnlyList<(string Role, string Title, string Description, IReadOnlyList<string> Items)> GetDefaults()
    {
        return new List<(string, string, string, IReadOnlyList<string>)>
        {
            (
                "Caja",
                "Checklist de caja",
                "Flujo esencial para operar una caja en modo entrenamiento.",
                new List<string>
                {
                    "Abrir sesion de operador y verificar dispositivo de caja",
                    "Abrir caja con monto inicial y elegir turno",
                    "Cobrar una venta mixta (efectivo + tarjeta)",
                    "Registrar un movimiento de caja con motivo",
                    "Cerrar caja y validar diferencias"
                }
            ),
            (
                "Tablet",
                "Checklist de tablet",
                "Operativa de armado y envio de carrito desde tablet.",
                new List<string>
                {
                    "Buscar productos por codigo rapido y por escaneo",
                    "Agregar item por peso y confirmar cantidad",
                    "Enviar carrito a caja destino",
                    "Editar carrito pendiente antes de convertir venta",
                    "Validar estado de sincronizacion online/offline"
                }
            ),
            (
                "Encargado",
                "Checklist de encargado",
                "Controles clave de operacion diaria para supervisores.",
                new List<string>
                {
                    "Revisar dashboard gerencial y pendientes",
                    "Gestionar reclamo a proveedor y registrar credito",
                    "Auditar una devolucion y su comprobante",
                    "Validar cierre de turno con tarea kanban requerida",
                    "Exportar resumen diario para seguimiento"
                }
            ),
            (
                "Dueño",
                "Checklist de dueño",
                "Recorrido ejecutivo de control del negocio.",
                new List<string>
                {
                    "Revisar ventas por rango y medios de pago",
                    "Controlar deudores de cuenta corriente y envases",
                    "Revisar stock critico y movimientos de merma",
                    "Auditar horas RRHH y fichadas abiertas",
                    "Descargar exportaciones gerenciales clave"
                }
            )
        };
    }
}

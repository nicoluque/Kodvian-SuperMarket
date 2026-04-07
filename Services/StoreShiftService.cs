using System.Text.Json;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using Microsoft.EntityFrameworkCore;

namespace KodvianSuperMarket.Services;

public class StoreShiftConfig
{
    public string Timezone { get; set; } = "America/Argentina/Buenos_Aires";
    public string MorningStart { get; set; } = "07:30";
    public string MorningCloseWindowStart { get; set; } = "14:00";
    public string MorningCloseWindowEnd { get; set; } = "15:00";
    public string AfternoonEnd { get; set; } = "22:00";
    public int GraceMinutes { get; set; } = 90;
}

public class ShiftAssignmentDecision
{
    public string AssignmentStatus { get; set; } = "Assigned";
    public string? ShiftBucket { get; set; }
    public string? ExpectedShiftBucket { get; set; }
    public string Reason { get; set; } = "AutoAssigned";
}

public class TransitionAutoAssignResult
{
    public int AssignedCount { get; set; }
    public bool LateShiftOpen { get; set; }
}

public interface IStoreShiftService
{
    Task<StoreShiftConfig> GetConfigAsync(int storeId);
    Task<bool> IsTotemQrOnlyStoreAsync(int storeId);
    Task<ShiftAssignmentDecision> ResolveSaleAssignmentAsync(int storeId, DateTime saleCreatedAtUtc);
    Task<TransitionAutoAssignResult> AssignTransitionsOnShiftOpenAsync(int storeId, string incomingShift, DateTime openedAtUtc, int? assignedByUsuarioId);
}

public class StoreShiftService : IStoreShiftService
{
    private readonly ApplicationDbContext _db;

    public StoreShiftService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StoreShiftConfig> GetConfigAsync(int storeId)
    {
        var settingsJson = await _db.Stores
            .Where(s => s.Id == storeId)
            .Select(s => s.SettingsJson)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(settingsJson))
            return new StoreShiftConfig();

        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            if (!doc.RootElement.TryGetProperty("shiftSchedule", out var shiftEl))
                return new StoreShiftConfig();

            return new StoreShiftConfig
            {
                Timezone = GetString(shiftEl, "timezone") ?? "America/Argentina/Buenos_Aires",
                MorningStart = GetString(shiftEl, "morningStart") ?? "07:30",
                MorningCloseWindowStart = GetString(shiftEl, "morningCloseWindowStart") ?? "14:00",
                MorningCloseWindowEnd = GetString(shiftEl, "morningCloseWindowEnd") ?? "15:00",
                AfternoonEnd = GetString(shiftEl, "afternoonEnd") ?? "22:00",
                GraceMinutes = GetInt(shiftEl, "graceMinutes") ?? 90
            };
        }
        catch
        {
            return new StoreShiftConfig();
        }
    }

    public async Task<bool> IsTotemQrOnlyStoreAsync(int storeId)
    {
        var settingsJson = await _db.Stores
            .Where(s => s.Id == storeId)
            .Select(s => s.SettingsJson)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(settingsJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(settingsJson);
            if (!doc.RootElement.TryGetProperty("operatingMode", out var modeEl))
                return false;

            return string.Equals(modeEl.GetString(), "TotemQrOnly", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task<ShiftAssignmentDecision> ResolveSaleAssignmentAsync(int storeId, DateTime saleCreatedAtUtc)
    {
        var cfg = await GetConfigAsync(storeId);
        var tz = ResolveTimezone(cfg.Timezone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(saleCreatedAtUtc, DateTimeKind.Utc), tz);

        var morningStart = ParseTime(cfg.MorningStart, new TimeSpan(7, 30, 0));
        var closeWindowStart = ParseTime(cfg.MorningCloseWindowStart, new TimeSpan(14, 0, 0));
        var afternoonEnd = ParseTime(cfg.AfternoonEnd, new TimeSpan(22, 0, 0));

        if (local.TimeOfDay >= closeWindowStart && local.TimeOfDay < afternoonEnd)
        {
            var hasOpenedAfternoon = await _db.CashSessions.AnyAsync(cs =>
                cs.StoreId == storeId &&
                cs.Shift == Shift.Afternoon.ToString() &&
                cs.OpenedAt <= saleCreatedAtUtc &&
                (cs.ClosedAt == null || cs.ClosedAt > saleCreatedAtUtc));

            if (hasOpenedAfternoon)
            {
                return new ShiftAssignmentDecision
                {
                    AssignmentStatus = "Assigned",
                    ShiftBucket = Shift.Afternoon.ToString(),
                    ExpectedShiftBucket = Shift.Afternoon.ToString(),
                    Reason = "AfternoonSessionOpen"
                };
            }

            return new ShiftAssignmentDecision
            {
                AssignmentStatus = "Transition",
                ShiftBucket = null,
                ExpectedShiftBucket = Shift.Afternoon.ToString(),
                Reason = "WaitingAfternoonOpen"
            };
        }

        if (local.TimeOfDay >= morningStart && local.TimeOfDay < closeWindowStart)
        {
            return new ShiftAssignmentDecision
            {
                AssignmentStatus = "Assigned",
                ShiftBucket = Shift.Morning.ToString(),
                ExpectedShiftBucket = Shift.Morning.ToString(),
                Reason = "MorningWindow"
            };
        }

        return new ShiftAssignmentDecision
        {
            AssignmentStatus = "Assigned",
            ShiftBucket = Shift.Night.ToString(),
            ExpectedShiftBucket = Shift.Night.ToString(),
            Reason = "NightWindow"
        };
    }

    public async Task<TransitionAutoAssignResult> AssignTransitionsOnShiftOpenAsync(int storeId, string incomingShift, DateTime openedAtUtc, int? assignedByUsuarioId)
    {
        var cfg = await GetConfigAsync(storeId);
        var tz = ResolveTimezone(cfg.Timezone);
        var localOpen = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(openedAtUtc, DateTimeKind.Utc), tz);

        var closeWindowEnd = ParseTime(cfg.MorningCloseWindowEnd, new TimeSpan(15, 0, 0));
        var isLate = string.Equals(incomingShift, Shift.Afternoon.ToString(), StringComparison.OrdinalIgnoreCase)
                     && localOpen.TimeOfDay > closeWindowEnd;

        var transitions = await _db.Sales
            .Where(s => s.StoreId == storeId
                        && s.ShiftAssignmentStatus == "Transition"
                        && s.ExpectedShiftBucket == incomingShift)
            .ToListAsync();

        if (transitions.Count == 0)
            return new TransitionAutoAssignResult { AssignedCount = 0, LateShiftOpen = isLate };

        var now = DateTime.UtcNow;
        foreach (var sale in transitions)
        {
            sale.ShiftAssignmentStatus = "Assigned";
            sale.ShiftBucket = incomingShift;
            sale.ShiftAssignedAt = now;
            sale.ShiftAssignedByUsuarioId = assignedByUsuarioId;
            sale.ShiftAssignmentReason = "AutoAssignedOnShiftOpen";
            sale.LateShiftOpen = isLate;
        }

        await _db.SaveChangesAsync();
        return new TransitionAutoAssignResult { AssignedCount = transitions.Count, LateShiftOpen = isLate };
    }

    private static string? GetString(JsonElement element, string key)
    {
        return element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? GetInt(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n)) return n;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed)) return parsed;
        return null;
    }

    private static TimeZoneInfo ResolveTimezone(string tz)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(tz);
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private static TimeSpan ParseTime(string raw, TimeSpan fallback)
    {
        return TimeSpan.TryParse(raw, out var value) ? value : fallback;
    }
}

using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;
using KodvianSuperMarket.DTOs;

namespace KodvianSuperMarket.Services;

public interface IRRHHService
{
    Task<TimePunch> PunchAsync(int usuarioId, int deviceId, int? cashSessionId, int? operatorSessionId);
    Task<List<TimePunchDto>> GetPunchesAsync(int? usuarioId, DateTime? from, DateTime? to);
    Task<TimePunch?> GetOpenPunchAsync(int usuarioId);
    Task<TimePunch> AdjustPunchAsync(int punchId, int adjustedById, DateTime newPunchTime, string reason);
    Task<List<TimePunchInconsistencyDto>> GetInconsistenciesAsync(DateTime? from, DateTime? to);
    Task<EmployeeExtra> CreateExtraAsync(int usuarioId, int createdById, DateTime extraDate, decimal hours, string reason);
    Task<EmployeeExtra> ApproveExtraAsync(int extraId, int approvedById, bool approve);
    Task<List<EmployeeExtraDto>> GetExtrasAsync(int? usuarioId, int? year, int? month, bool? approved);
    Task<PayrollReceipt> UploadReceiptAsync(int usuarioId, int year, int month, string fileName, byte[] fileContent);
    Task<List<PayrollReceiptDto>> GetReceiptsAsync(int? usuarioId, int? year, int? month);
    Task<PayrollReceipt?> GetReceiptAsync(int id);
    Task<byte[]> DownloadReceiptAsync(int id);
}

public class RRHHService : IRRHHService
{
    private readonly ApplicationDbContext _context;

    public RRHHService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TimePunch> PunchAsync(int usuarioId, int deviceId, int? cashSessionId, int? operatorSessionId)
    {
        var openEntry = await _context.TimePunches
            .Where(tp => tp.UsuarioId == usuarioId && tp.PunchType == TimePunchType.Entry.ToString() && tp.IsOpen)
            .OrderByDescending(tp => tp.PunchTime)
            .FirstOrDefaultAsync();

        var isEntry = openEntry == null;

        if (openEntry != null)
            openEntry.IsOpen = false;

        var punch = new TimePunch
        {
            UsuarioId = usuarioId,
            DeviceId = deviceId,
            CashSessionId = cashSessionId,
            OperatorSessionId = operatorSessionId,
            PunchType = isEntry ? TimePunchType.Entry.ToString() : TimePunchType.Exit.ToString(),
            IsOpen = isEntry,
            PunchTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.TimePunches.Add(punch);
        await _context.SaveChangesAsync();

        return punch;
    }

    public async Task<List<TimePunchDto>> GetPunchesAsync(int? usuarioId, DateTime? from, DateTime? to)
    {
        var query = _context.TimePunches
            .Include(tp => tp.Usuario)
            .Include(tp => tp.Device)
            .Include(tp => tp.AdjustedBy)
            .AsQueryable();

        if (usuarioId.HasValue)
            query = query.Where(tp => tp.UsuarioId == usuarioId.Value);

        if (from.HasValue)
            query = query.Where(tp => tp.PunchTime >= from.Value);

        if (to.HasValue)
            query = query.Where(tp => tp.PunchTime <= to.Value);

        return await query
            .OrderByDescending(tp => tp.PunchTime)
            .Select(tp => new TimePunchDto
            {
                Id = tp.Id,
                UsuarioId = tp.UsuarioId,
                UsuarioName = tp.Usuario != null ? tp.Usuario.Username : null,
                CashSessionId = tp.CashSessionId,
                DeviceId = tp.DeviceId,
                DeviceName = tp.Device != null ? tp.Device.DeviceName : null,
                PunchType = tp.PunchType,
                PunchTime = tp.PunchTime,
                IsAdjusted = tp.IsAdjusted,
                AdjustedAt = tp.AdjustedAt,
                AdjustedById = tp.AdjustedById,
                AdjustedByName = tp.AdjustedBy != null ? tp.AdjustedBy.Username : null,
                CreatedAt = tp.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TimePunch?> GetOpenPunchAsync(int usuarioId)
    {
        return await _context.TimePunches
            .Where(tp => tp.UsuarioId == usuarioId && tp.PunchType == TimePunchType.Entry.ToString() && tp.IsOpen)
            .OrderByDescending(tp => tp.PunchTime)
            .FirstOrDefaultAsync();
    }

    public async Task<TimePunch> AdjustPunchAsync(int punchId, int adjustedById, DateTime newPunchTime, string reason)
    {
        var punch = await _context.TimePunches.FindAsync(punchId);
        if (punch == null)
            throw new InvalidOperationException("Time punch not found");

        var originalTime = punch.PunchTime;
        punch.PunchTime = newPunchTime;
        punch.IsAdjusted = true;
        punch.AdjustedAt = DateTime.UtcNow;
        punch.AdjustedById = adjustedById;

        var adjustment = new TimePunchAdjustment
        {
            TimePunchId = punchId,
            AdjustedById = adjustedById,
            OriginalPunchTime = originalTime,
            NewPunchTime = newPunchTime,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        _context.TimePunchAdjustments.Add(adjustment);
        await _context.SaveChangesAsync();

        return punch;
    }

    public async Task<List<TimePunchInconsistencyDto>> GetInconsistenciesAsync(DateTime? from, DateTime? to)
    {
        var startDate = from ?? DateTime.UtcNow.Date.AddDays(-7);
        var endDate = to ?? DateTime.UtcNow.Date.AddDays(1);

        var punches = await _context.TimePunches
            .Include(tp => tp.Usuario)
            .Where(tp => tp.PunchTime >= startDate && tp.PunchTime < endDate)
            .OrderBy(tp => tp.UsuarioId)
            .ThenBy(tp => tp.PunchTime)
            .ToListAsync();

        var inconsistencies = new List<TimePunchInconsistencyDto>();
        var groupedPunches = punches.GroupBy(tp => tp.UsuarioId);

        foreach (var group in groupedPunches)
        {
            var userPunches = group.ToList();
            var dates = userPunches.Select(tp => tp.PunchTime.Date).Distinct().OrderBy(d => d);

            foreach (var date in dates)
            {
                var dayPunches = userPunches.Where(tp => tp.PunchTime.Date == date).OrderBy(tp => tp.PunchTime).ToList();
                var entries = dayPunches.Where(tp => tp.PunchType == TimePunchType.Entry.ToString()).ToList();
                var exits = dayPunches.Where(tp => tp.PunchType == TimePunchType.Exit.ToString()).ToList();

                if (entries.Count == 0 && exits.Count > 0)
                {
                    inconsistencies.Add(new TimePunchInconsistencyDto
                    {
                        UsuarioId = group.Key,
                        UsuarioName = userPunches.First().Usuario?.Username,
                        Date = date,
                        ExitTime = exits.First().PunchTime,
                        InconsistencyType = "MissingEntry",
                        Description = "Exit without entry"
                    });
                }
                else if (entries.Count > exits.Count)
                {
                    var lastEntry = entries.Last();
                    if (!exits.Any(e => e.PunchTime > lastEntry.PunchTime))
                    {
                        inconsistencies.Add(new TimePunchInconsistencyDto
                        {
                            UsuarioId = group.Key,
                            UsuarioName = userPunches.First().Usuario?.Username,
                            Date = date,
                            EntryTime = lastEntry.PunchTime,
                            InconsistencyType = "MissingExit",
                            Description = "Entry without exit"
                        });
                    }
                }
                else if (entries.Count > 1)
                {
                    inconsistencies.Add(new TimePunchInconsistencyDto
                    {
                        UsuarioId = group.Key,
                        UsuarioName = userPunches.First().Usuario?.Username,
                        Date = date,
                        EntryTime = entries.First().PunchTime,
                        InconsistencyType = "MultipleEntries",
                        Description = $"Multiple entries ({entries.Count})"
                    });
                }
            }
        }

        return inconsistencies;
    }

    public async Task<EmployeeExtra> CreateExtraAsync(int usuarioId, int createdById, DateTime extraDate, decimal hours, string reason)
    {
        var extra = new EmployeeExtra
        {
            UsuarioId = usuarioId,
            CreatedById = createdById,
            ExtraDate = extraDate,
            Year = extraDate.Year,
            Month = extraDate.Month,
            Hours = hours,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeExtras.Add(extra);
        await _context.SaveChangesAsync();

        return extra;
    }

    public async Task<EmployeeExtra> ApproveExtraAsync(int extraId, int approvedById, bool approve)
    {
        var extra = await _context.EmployeeExtras.FindAsync(extraId);
        if (extra == null)
            throw new InvalidOperationException("Employee extra not found");

        extra.IsApproved = approve;
        if (approve)
        {
            extra.ApprovedById = approvedById;
            extra.ApprovedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return extra;
    }

    public async Task<List<EmployeeExtraDto>> GetExtrasAsync(int? usuarioId, int? year, int? month, bool? approved)
    {
        var query = _context.EmployeeExtras
            .Include(ee => ee.Usuario)
            .Include(ee => ee.CreatedBy)
            .Include(ee => ee.ApprovedBy)
            .AsQueryable();

        if (usuarioId.HasValue)
            query = query.Where(ee => ee.UsuarioId == usuarioId.Value);

        if (year.HasValue)
            query = query.Where(ee => ee.Year == year.Value);

        if (month.HasValue)
            query = query.Where(ee => ee.Month == month.Value);

        if (approved.HasValue)
            query = query.Where(ee => ee.IsApproved == approved.Value);

        return await query
            .OrderByDescending(ee => ee.ExtraDate)
            .Select(ee => new EmployeeExtraDto
            {
                Id = ee.Id,
                UsuarioId = ee.UsuarioId,
                UsuarioName = ee.Usuario != null ? ee.Usuario.Username : null,
                CreatedById = ee.CreatedById,
                CreatedByName = ee.CreatedBy != null ? ee.CreatedBy.Username : null,
                ExtraDate = ee.ExtraDate,
                Year = ee.Year,
                Month = ee.Month,
                Hours = ee.Hours,
                Reason = ee.Reason,
                IsApproved = ee.IsApproved,
                ApprovedById = ee.ApprovedById,
                ApprovedByName = ee.ApprovedBy != null ? ee.ApprovedBy.Username : null,
                ApprovedAt = ee.ApprovedAt,
                CreatedAt = ee.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<PayrollReceipt> UploadReceiptAsync(int usuarioId, int year, int month, string fileName, byte[] fileContent)
    {
        var existing = await _context.PayrollReceipts
            .FirstOrDefaultAsync(pr => pr.UsuarioId == usuarioId && pr.Year == year && pr.Month == month);

        if (existing != null)
        {
            existing.FileName = fileName;
            existing.FileContent = fileContent;
            existing.FileSize = fileContent.Length;
            await _context.SaveChangesAsync();
            return existing;
        }

        var receipt = new PayrollReceipt
        {
            UsuarioId = usuarioId,
            Year = year,
            Month = month,
            FileName = fileName,
            FileContent = fileContent,
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            CreatedAt = DateTime.UtcNow
        };

        _context.PayrollReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        return receipt;
    }

    public async Task<List<PayrollReceiptDto>> GetReceiptsAsync(int? usuarioId, int? year, int? month)
    {
        var query = _context.PayrollReceipts
            .Include(pr => pr.Usuario)
            .AsQueryable();

        if (usuarioId.HasValue)
            query = query.Where(pr => pr.UsuarioId == usuarioId.Value);

        if (year.HasValue)
            query = query.Where(pr => pr.Year == year.Value);

        if (month.HasValue)
            query = query.Where(pr => pr.Month == month.Value);

        return await query
            .OrderByDescending(pr => pr.Year)
            .ThenByDescending(pr => pr.Month)
            .Select(pr => new PayrollReceiptDto
            {
                Id = pr.Id,
                UsuarioId = pr.UsuarioId,
                UsuarioName = pr.Usuario != null ? pr.Usuario.Username : null,
                Year = pr.Year,
                Month = pr.Month,
                FileName = pr.FileName,
                FileSize = pr.FileSize,
                CreatedAt = pr.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<PayrollReceipt?> GetReceiptAsync(int id)
    {
        return await _context.PayrollReceipts.FindAsync(id);
    }

    public async Task<byte[]> DownloadReceiptAsync(int id)
    {
        var receipt = await _context.PayrollReceipts.FindAsync(id);
        if (receipt == null)
            throw new InvalidOperationException("Payroll receipt not found");

        return receipt.FileContent;
    }
}

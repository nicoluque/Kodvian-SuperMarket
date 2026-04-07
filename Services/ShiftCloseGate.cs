using Microsoft.EntityFrameworkCore;
using KodvianSuperMarket.Data;
using KodvianSuperMarket.Models;

namespace KodvianSuperMarket.Services;

public interface IShiftCloseGate
{
    Task<List<string>> GetBlockedByTasksAsync(int cashSessionId);
    Task<List<string>> GetMissingRequiredTasksAsync(int cashSessionId);
}

public class RealShiftCloseGate : IShiftCloseGate
{
    private readonly ApplicationDbContext _context;

    public RealShiftCloseGate(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<string>> GetBlockedByTasksAsync(int cashSessionId)
    {
        return GetMissingRequiredTasksAsync(cashSessionId);
    }

    public async Task<List<string>> GetMissingRequiredTasksAsync(int cashSessionId)
    {
        var tasks = await _context.KanbanTasks
            .Where(t => t.CashSessionId == cashSessionId && t.IsRequiredForShiftClose)
            .ToListAsync();

        var missing = new List<string>();
        foreach (var task in tasks)
        {
            var checklist = await _context.KanbanChecklistItems
                .Where(c => c.KanbanTaskId == task.Id)
                .ToListAsync();

            var isDone = task.Status == KanbanTaskStatus.Done.ToString();
            var checklistDone = checklist.Count == 0 || checklist.All(c => c.IsDone);

            if (!isDone || !checklistDone)
                missing.Add(task.Title);
        }

        return missing;
    }
}

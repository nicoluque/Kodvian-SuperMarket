using KodvianSuperMarket.Data;
using KodvianSuperMarket.DTOs;
using KodvianSuperMarket.Models;
using Microsoft.EntityFrameworkCore;

namespace KodvianSuperMarket.Services;

public interface IKanbanService
{
    Task GenerateForCashSessionAsync(int cashSessionId);
    Task<List<KanbanBoardDto>> GetBoardsAsync();
    Task<KanbanBoardDto> CreateBoardAsync(string name, string? description);
    Task<KanbanBoardDto> UpdateBoardAsync(int id, string name, string? description, bool isActive);
    Task<List<KanbanTemplateDto>> GetTemplatesAsync(int? boardId = null, bool? active = null);
    Task<KanbanTemplateDto> CreateTemplateAsync(
        int boardId,
        string name,
        string? description,
        string? shift,
        bool requiredForClose,
        List<(string text, int sortOrder)> checklistItems,
        List<(string frequency, int? dayOfWeek)> recurrenceRules);
    Task<KanbanTemplateDto> UpdateTemplateAsync(int id, string name, string? description, string? shift, bool requiredForClose, bool isActive);
    Task<List<KanbanTaskDto>> GetTasksAsync(int? cashSessionId = null, int? boardId = null, string? status = null, bool? requiredOnly = null);
    Task<KanbanTaskDto> UpdateTaskStatusAsync(int taskId, string status, int updatedByUsuarioId);
    Task<KanbanChecklistItemDto> ToggleChecklistItemAsync(int checklistItemId, bool isDone, int usuarioId);
    Task<KanbanCommentDto> AddCommentAsync(int taskId, int usuarioId, string text);
    Task<List<KanbanCommentDto>> GetCommentsAsync(int taskId);
}

public class KanbanService : IKanbanService
{
    private readonly ApplicationDbContext _context;

    public KanbanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task GenerateForCashSessionAsync(int cashSessionId)
    {
        await EnsureInitialSeedAsync();

        var session = await _context.Set<CashSession>().FirstOrDefaultAsync(x => x.Id == cashSessionId);
        if (session == null)
        {
            throw new InvalidOperationException("Cash session not found.");
        }

        var generationDate = session.OpenedAt.Date;
        var sessionShift = NormalizeShift(session.Shift) ?? string.Empty;

        var templates = await _context.Set<KanbanTemplate>()
            .Include(t => t.ChecklistItems)
            .Include(t => t.RecurrenceRules)
            .Where(t => t.IsActive)
            .ToListAsync();

        foreach (var template in templates)
        {
            var templateShift = NormalizeShift(template.Shift);
            if (!string.IsNullOrWhiteSpace(templateShift) && !string.Equals(templateShift, sessionShift, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!AppliesByRecurrence(template, generationDate))
            {
                continue;
            }

            var duplicateExists = await _context.Set<GeneratedTask>().AnyAsync(g =>
                g.CashSessionId == cashSessionId
                && g.KanbanTemplateId == template.Id
                && g.GenerationDate == generationDate
                && g.Shift == sessionShift);

            if (duplicateExists)
            {
                continue;
            }

            var task = new KanbanTask
            {
                KanbanBoardId = template.KanbanBoardId,
                CashSessionId = session.Id,
                StoreId = session.StoreId,
                KanbanTemplateId = template.Id,
                Title = template.Name,
                Description = template.Description,
                Status = KanbanTaskStatus.Pending.ToString(),
                IsRequiredForShiftClose = template.IsRequiredForShiftClose,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<KanbanTask>().Add(task);
            await _context.SaveChangesAsync();

            var orderedChecklist = template.ChecklistItems
                .OrderBy(i => i.SortOrder)
                .Select(i => new KanbanChecklistItem
                {
                    KanbanTaskId = task.Id,
                    Text = i.Text,
                    IsDone = false
                })
                .ToList();

            if (orderedChecklist.Count > 0)
            {
                _context.Set<KanbanChecklistItem>().AddRange(orderedChecklist);
            }

            _context.Set<GeneratedTask>().Add(new GeneratedTask
            {
                CashSessionId = session.Id,
                KanbanTemplateId = template.Id,
                GenerationDate = generationDate,
                Shift = sessionShift,
                KanbanTaskId = task.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<KanbanBoardDto>> GetBoardsAsync()
    {
        return await _context.Set<KanbanBoard>()
            .OrderBy(b => b.Name)
            .Select(MapBoardDtoExpression())
            .ToListAsync();
    }

    public async Task<KanbanBoardDto> CreateBoardAsync(string name, string? description)
    {
        var board = new KanbanBoard
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<KanbanBoard>().Add(board);
        await _context.SaveChangesAsync();
        return MapBoardDto(board);
    }

    public async Task<KanbanBoardDto> UpdateBoardAsync(int id, string name, string? description, bool isActive)
    {
        var board = await _context.Set<KanbanBoard>().FirstOrDefaultAsync(b => b.Id == id);
        if (board == null)
        {
            throw new InvalidOperationException("Board not found.");
        }

        board.Name = name.Trim();
        board.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        board.IsActive = isActive;

        await _context.SaveChangesAsync();
        return MapBoardDto(board);
    }

    public async Task<List<KanbanTemplateDto>> GetTemplatesAsync(int? boardId = null, bool? active = null)
    {
        var query = _context.Set<KanbanTemplate>()
            .Include(t => t.ChecklistItems)
            .Include(t => t.RecurrenceRules)
            .AsQueryable();

        if (boardId.HasValue)
        {
            query = query.Where(t => t.KanbanBoardId == boardId.Value);
        }

        if (active.HasValue)
        {
            query = query.Where(t => t.IsActive == active.Value);
        }

        var templates = await query
            .OrderBy(t => t.Name)
            .ToListAsync();

        return templates.Select(MapTemplateDto).ToList();
    }

    public async Task<KanbanTemplateDto> CreateTemplateAsync(
        int boardId,
        string name,
        string? description,
        string? shift,
        bool requiredForClose,
        List<(string text, int sortOrder)> checklistItems,
        List<(string frequency, int? dayOfWeek)> recurrenceRules)
    {
        var boardExists = await _context.Set<KanbanBoard>().AnyAsync(b => b.Id == boardId);
        if (!boardExists)
        {
            throw new InvalidOperationException("Board not found.");
        }

        var template = new KanbanTemplate
        {
            KanbanBoardId = boardId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Shift = NormalizeShift(shift),
            IsRequiredForShiftClose = requiredForClose,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<KanbanTemplate>().Add(template);
        await _context.SaveChangesAsync();

        var checklist = checklistItems
            .Where(i => !string.IsNullOrWhiteSpace(i.text))
            .Select(i => new TemplateChecklistItem
            {
                KanbanTemplateId = template.Id,
                Text = i.text.Trim(),
                SortOrder = i.sortOrder
            })
            .ToList();

        if (checklist.Count > 0)
        {
            _context.Set<TemplateChecklistItem>().AddRange(checklist);
        }

        var rules = recurrenceRules
            .Where(r => !string.IsNullOrWhiteSpace(r.frequency))
            .Select(r => new RecurrenceRule
            {
                KanbanTemplateId = template.Id,
                Frequency = NormalizeFrequency(r.frequency),
                DayOfWeek = r.dayOfWeek,
                IsActive = true
            })
            .ToList();

        if (rules.Count == 0)
        {
            rules.Add(new RecurrenceRule
            {
                KanbanTemplateId = template.Id,
                Frequency = RecurrenceType.Daily.ToString(),
                DayOfWeek = null,
                IsActive = true
            });
        }

        _context.Set<RecurrenceRule>().AddRange(rules);
        await _context.SaveChangesAsync();

        var created = await _context.Set<KanbanTemplate>()
            .Include(t => t.ChecklistItems)
            .Include(t => t.RecurrenceRules)
            .FirstAsync(t => t.Id == template.Id);

        return MapTemplateDto(created);
    }

    public async Task<KanbanTemplateDto> UpdateTemplateAsync(int id, string name, string? description, string? shift, bool requiredForClose, bool isActive)
    {
        var template = await _context.Set<KanbanTemplate>()
            .Include(t => t.ChecklistItems)
            .Include(t => t.RecurrenceRules)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            throw new InvalidOperationException("Template not found.");
        }

        template.Name = name.Trim();
        template.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        template.Shift = NormalizeShift(shift);
        template.IsRequiredForShiftClose = requiredForClose;
        template.IsActive = isActive;

        if (!template.RecurrenceRules.Any())
        {
            template.RecurrenceRules.Add(new RecurrenceRule
            {
                KanbanTemplateId = template.Id,
                Frequency = RecurrenceType.Daily.ToString(),
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
        return MapTemplateDto(template);
    }

    public async Task<List<KanbanTaskDto>> GetTasksAsync(int? cashSessionId = null, int? boardId = null, string? status = null, bool? requiredOnly = null)
    {
        var query = _context.Set<KanbanTask>()
            .Include(t => t.ChecklistItems)
            .AsQueryable();

        if (cashSessionId.HasValue)
        {
            query = query.Where(t => t.CashSessionId == cashSessionId.Value);
        }

        if (boardId.HasValue)
        {
            query = query.Where(t => t.KanbanBoardId == boardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (requiredOnly.HasValue && requiredOnly.Value)
        {
            query = query.Where(t => t.IsRequiredForShiftClose);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tasks.Select(MapTaskDto).ToList();
    }

    public async Task<KanbanTaskDto> UpdateTaskStatusAsync(int taskId, string status, int updatedByUsuarioId)
    {
        if (!IsValidTaskStatus(status))
        {
            throw new InvalidOperationException("Invalid task status.");
        }

        var task = await _context.Set<KanbanTask>()
            .Include(t => t.ChecklistItems)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found.");
        }

        task.Status = status;
        task.UpdatedByUsuarioId = updatedByUsuarioId;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapTaskDto(task);
    }

    public async Task<KanbanChecklistItemDto> ToggleChecklistItemAsync(int checklistItemId, bool isDone, int usuarioId)
    {
        var item = await _context.Set<KanbanChecklistItem>().FirstOrDefaultAsync(i => i.Id == checklistItemId);
        if (item == null)
        {
            throw new InvalidOperationException("Checklist item not found.");
        }

        item.IsDone = isDone;
        item.DoneAt = isDone ? DateTime.UtcNow : null;
        item.DoneByUsuarioId = isDone ? usuarioId : null;

        await _context.SaveChangesAsync();
        return MapChecklistItemDto(item);
    }

    public async Task<KanbanCommentDto> AddCommentAsync(int taskId, int usuarioId, string text)
    {
        var taskExists = await _context.Set<KanbanTask>().AnyAsync(t => t.Id == taskId);
        if (!taskExists)
        {
            throw new InvalidOperationException("Task not found.");
        }

        var comment = new KanbanComment
        {
            KanbanTaskId = taskId,
            UsuarioId = usuarioId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<KanbanComment>().Add(comment);
        await _context.SaveChangesAsync();

        var usuario = await _context.Set<Usuario>().FirstOrDefaultAsync(u => u.Id == usuarioId);

        return new KanbanCommentDto
        {
            Id = comment.Id,
            UsuarioId = comment.UsuarioId,
            UsuarioName = usuario?.Username,
            Text = comment.Text,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<List<KanbanCommentDto>> GetCommentsAsync(int taskId)
    {
        return await _context.Set<KanbanComment>()
            .Where(c => c.KanbanTaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new KanbanCommentDto
            {
                Id = c.Id,
                UsuarioId = c.UsuarioId,
                UsuarioName = c.Usuario.Username,
                Text = c.Text,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    private async Task EnsureInitialSeedAsync()
    {
        var board = await _context.Set<KanbanBoard>()
            .FirstOrDefaultAsync(b => b.Name == "Caja");

        if (board == null)
        {
            board = new KanbanBoard
            {
                Name = "Caja",
                Description = "Tablero operativo para tareas de caja.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<KanbanBoard>().Add(board);
            await _context.SaveChangesAsync();
        }

        var template = await _context.Set<KanbanTemplate>()
            .Include(t => t.ChecklistItems)
            .Include(t => t.RecurrenceRules)
            .FirstOrDefaultAsync(t => t.KanbanBoardId == board.Id && t.Name == "Cierre de turno - Caja");

        if (template == null)
        {
            template = new KanbanTemplate
            {
                KanbanBoardId = board.Id,
                Name = "Cierre de turno - Caja",
                Description = "Checklist obligatoria para el cierre de turno de caja.",
                Shift = null,
                IsRequiredForShiftClose = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Set<KanbanTemplate>().Add(template);
            await _context.SaveChangesAsync();
        }
        else
        {
            template.IsRequiredForShiftClose = true;
            template.IsActive = true;
            template.Shift = null;
            await _context.SaveChangesAsync();
        }

        if (!template.ChecklistItems.Any())
        {
            var seedChecklist = new List<TemplateChecklistItem>
            {
                new() { KanbanTemplateId = template.Id, Text = "Contar efectivo en caja", SortOrder = 1 },
                new() { KanbanTemplateId = template.Id, Text = "Verificar ventas en POS", SortOrder = 2 },
                new() { KanbanTemplateId = template.Id, Text = "Registrar diferencias y observaciones", SortOrder = 3 },
                new() { KanbanTemplateId = template.Id, Text = "Dejar fondo inicial del siguiente turno", SortOrder = 4 }
            };

            _context.Set<TemplateChecklistItem>().AddRange(seedChecklist);
            await _context.SaveChangesAsync();
        }

        if (!template.RecurrenceRules.Any(r => r.IsActive))
        {
            _context.Set<RecurrenceRule>().Add(new RecurrenceRule
            {
                KanbanTemplateId = template.Id,
                Frequency = RecurrenceType.Daily.ToString(),
                DayOfWeek = null,
                IsActive = true
            });

            await _context.SaveChangesAsync();
        }
    }

    private static bool AppliesByRecurrence(KanbanTemplate template, DateTime generationDate)
    {
        var activeRules = template.RecurrenceRules.Where(r => r.IsActive).ToList();
        if (activeRules.Count == 0)
        {
            return true;
        }

        foreach (var rule in activeRules)
        {
            if (string.Equals(rule.Frequency, RecurrenceType.Daily.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(rule.Frequency, RecurrenceType.Weekly.ToString(), StringComparison.OrdinalIgnoreCase)
                && rule.DayOfWeek.HasValue
                && rule.DayOfWeek.Value == (int)generationDate.DayOfWeek)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidTaskStatus(string status)
    {
        return Enum.TryParse<KanbanTaskStatus>(status, true, out _);
    }

    private static string NormalizeFrequency(string frequency)
    {
        if (Enum.TryParse<RecurrenceType>(frequency, true, out var parsed))
        {
            return parsed.ToString();
        }

        return RecurrenceType.Daily.ToString();
    }

    private static string? NormalizeShift(string? shift)
    {
        return string.IsNullOrWhiteSpace(shift) ? null : shift.Trim();
    }

    private static System.Linq.Expressions.Expression<Func<KanbanBoard, KanbanBoardDto>> MapBoardDtoExpression()
    {
        return b => new KanbanBoardDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            IsActive = b.IsActive
        };
    }

    private static KanbanBoardDto MapBoardDto(KanbanBoard board)
    {
        return new KanbanBoardDto
        {
            Id = board.Id,
            Name = board.Name,
            Description = board.Description,
            IsActive = board.IsActive
        };
    }

    private static KanbanTemplateDto MapTemplateDto(KanbanTemplate template)
    {
        return new KanbanTemplateDto
        {
            Id = template.Id,
            KanbanBoardId = template.KanbanBoardId,
            Name = template.Name,
            Description = template.Description,
            Shift = template.Shift,
            IsRequiredForShiftClose = template.IsRequiredForShiftClose,
            IsActive = template.IsActive,
            ChecklistItems = template.ChecklistItems
                .OrderBy(i => i.SortOrder)
                .Select(i => new TemplateChecklistItemDto
                {
                    Id = i.Id,
                    Text = i.Text,
                    SortOrder = i.SortOrder
                })
                .ToList(),
            RecurrenceRules = template.RecurrenceRules
                .Select(r => new RecurrenceRuleDto
                {
                    Id = r.Id,
                    Frequency = r.Frequency,
                    DayOfWeek = r.DayOfWeek,
                    IsActive = r.IsActive
                })
                .ToList()
        };
    }

    private static KanbanTaskDto MapTaskDto(KanbanTask task)
    {
        return new KanbanTaskDto
        {
            Id = task.Id,
            KanbanBoardId = task.KanbanBoardId,
            CashSessionId = task.CashSessionId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            IsRequiredForShiftClose = task.IsRequiredForShiftClose,
            CreatedAt = task.CreatedAt,
            ChecklistItems = task.ChecklistItems
                .Select(MapChecklistItemDto)
                .ToList()
        };
    }

    private static KanbanChecklistItemDto MapChecklistItemDto(KanbanChecklistItem item)
    {
        return new KanbanChecklistItemDto
        {
            Id = item.Id,
            Text = item.Text,
            IsDone = item.IsDone,
            DoneAt = item.DoneAt
        };
    }
}

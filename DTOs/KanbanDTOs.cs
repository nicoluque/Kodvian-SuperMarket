using System.ComponentModel.DataAnnotations;

namespace KodvianSuperMarket.DTOs;

public class KanbanBoardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class CreateKanbanBoardDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateKanbanBoardDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class KanbanTemplateDto
{
    public int Id { get; set; }
    public int KanbanBoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Shift { get; set; }
    public bool IsRequiredForShiftClose { get; set; }
    public bool IsActive { get; set; }
    public List<TemplateChecklistItemDto> ChecklistItems { get; set; } = new();
    public List<RecurrenceRuleDto> RecurrenceRules { get; set; } = new();
}

public class CreateKanbanTemplateDto
{
    [Required]
    public int KanbanBoardId { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Shift { get; set; }
    public bool IsRequiredForShiftClose { get; set; }
    public List<CreateTemplateChecklistItemDto> ChecklistItems { get; set; } = new();
    public List<CreateRecurrenceRuleDto> RecurrenceRules { get; set; } = new();
}

public class UpdateKanbanTemplateDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Shift { get; set; }
    public bool IsRequiredForShiftClose { get; set; }
    public bool IsActive { get; set; }
}

public class TemplateChecklistItemDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class CreateTemplateChecklistItemDto
{
    [Required]
    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class RecurrenceRuleDto
{
    public int Id { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public int? DayOfWeek { get; set; }
    public bool IsActive { get; set; }
}

public class CreateRecurrenceRuleDto
{
    [Required]
    public string Frequency { get; set; } = string.Empty;
    public int? DayOfWeek { get; set; }
}

public class KanbanTaskDto
{
    public int Id { get; set; }
    public int KanbanBoardId { get; set; }
    public int CashSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsRequiredForShiftClose { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<KanbanChecklistItemDto> ChecklistItems { get; set; } = new();
}

public class KanbanChecklistItemDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime? DoneAt { get; set; }
}

public class UpdateKanbanTaskStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public class ToggleChecklistItemDto
{
    [Required]
    public bool IsDone { get; set; }
}

public class CreateKanbanCommentDto
{
    [Required]
    public string Text { get; set; } = string.Empty;
}

public class KanbanCommentDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string? UsuarioName { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

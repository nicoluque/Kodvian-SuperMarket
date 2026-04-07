using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum KanbanTaskStatus
{
    Pending,
    InProgress,
    Blocked,
    Done
}

public enum RecurrenceType
{
    Daily,
    Weekly
}

public class KanbanBoard
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<KanbanTask> Tasks { get; set; } = new List<KanbanTask>();
    public ICollection<KanbanTemplate> Templates { get; set; } = new List<KanbanTemplate>();
}

public class KanbanTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KanbanBoardId { get; set; }

    [ForeignKey(nameof(KanbanBoardId))]
    public KanbanBoard KanbanBoard { get; set; } = null!;

    [Required]
    public int CashSessionId { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession CashSession { get; set; } = null!;

    public int? KanbanTemplateId { get; set; }

    [ForeignKey(nameof(KanbanTemplateId))]
    public KanbanTemplate? KanbanTemplate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = KanbanTaskStatus.Pending.ToString();

    public bool IsRequiredForShiftClose { get; set; } = false;

    public int? AssignedToUsuarioId { get; set; }

    [ForeignKey(nameof(AssignedToUsuarioId))]
    public Usuario? AssignedToUsuario { get; set; }

    public int? UpdatedByUsuarioId { get; set; }

    [ForeignKey(nameof(UpdatedByUsuarioId))]
    public Usuario? UpdatedByUsuario { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<KanbanChecklistItem> ChecklistItems { get; set; } = new List<KanbanChecklistItem>();
    public ICollection<KanbanComment> Comments { get; set; } = new List<KanbanComment>();
}

public class KanbanChecklistItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KanbanTaskId { get; set; }

    [ForeignKey(nameof(KanbanTaskId))]
    public KanbanTask KanbanTask { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    public bool IsDone { get; set; } = false;
    public DateTime? DoneAt { get; set; }

    public int? DoneByUsuarioId { get; set; }

    [ForeignKey(nameof(DoneByUsuarioId))]
    public Usuario? DoneByUsuario { get; set; }
}

public class KanbanComment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KanbanTaskId { get; set; }

    [ForeignKey(nameof(KanbanTaskId))]
    public KanbanTask KanbanTask { get; set; } = null!;

    [Required]
    public int UsuarioId { get; set; }

    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class KanbanTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KanbanBoardId { get; set; }

    [ForeignKey(nameof(KanbanBoardId))]
    public KanbanBoard KanbanBoard { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Shift { get; set; }

    public bool IsRequiredForShiftClose { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TemplateChecklistItem> ChecklistItems { get; set; } = new List<TemplateChecklistItem>();
    public ICollection<RecurrenceRule> RecurrenceRules { get; set; } = new List<RecurrenceRule>();
}

public class TemplateChecklistItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KanbanTemplateId { get; set; }

    [ForeignKey(nameof(KanbanTemplateId))]
    public KanbanTemplate KanbanTemplate { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
}

public class RecurrenceRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int KanbanTemplateId { get; set; }

    [ForeignKey(nameof(KanbanTemplateId))]
    public KanbanTemplate KanbanTemplate { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Frequency { get; set; } = RecurrenceType.Daily.ToString();

    public int? DayOfWeek { get; set; }
    public bool IsActive { get; set; } = true;
}

public class GeneratedTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CashSessionId { get; set; }

    [ForeignKey(nameof(CashSessionId))]
    public CashSession CashSession { get; set; } = null!;

    [Required]
    public int KanbanTemplateId { get; set; }

    [ForeignKey(nameof(KanbanTemplateId))]
    public KanbanTemplate KanbanTemplate { get; set; } = null!;

    [Required]
    public DateTime GenerationDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Shift { get; set; } = string.Empty;

    [Required]
    public int KanbanTaskId { get; set; }

    [ForeignKey(nameof(KanbanTaskId))]
    public KanbanTask KanbanTask { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

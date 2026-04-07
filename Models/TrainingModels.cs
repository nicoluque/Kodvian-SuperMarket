using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum TrainingRunStatus
{
    InProgress,
    Completed
}

public class TrainingChecklist
{
    [Key]
    public int Id { get; set; }

    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TrainingChecklistItem> Items { get; set; } = new List<TrainingChecklistItem>();
}

public class TrainingChecklistItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TrainingChecklistId { get; set; }

    [ForeignKey(nameof(TrainingChecklistId))]
    public TrainingChecklist TrainingChecklist { get; set; } = null!;

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TrainingChecklistRun
{
    [Key]
    public int Id { get; set; }

    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Required]
    public int TrainingChecklistId { get; set; }

    [ForeignKey(nameof(TrainingChecklistId))]
    public TrainingChecklist TrainingChecklist { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    public int? StartedByUsuarioId { get; set; }

    [ForeignKey(nameof(StartedByUsuarioId))]
    public Usuario? StartedByUsuario { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = TrainingRunStatus.InProgress.ToString();

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public ICollection<TrainingChecklistRunItem> Items { get; set; } = new List<TrainingChecklistRunItem>();
}

public class TrainingChecklistRunItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TrainingChecklistRunId { get; set; }

    [ForeignKey(nameof(TrainingChecklistRunId))]
    public TrainingChecklistRun TrainingChecklistRun { get; set; } = null!;

    [Required]
    public int TrainingChecklistItemId { get; set; }

    [ForeignKey(nameof(TrainingChecklistItemId))]
    public TrainingChecklistItem TrainingChecklistItem { get; set; } = null!;

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedAt { get; set; }
}

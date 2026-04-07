using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public enum OnboardingStatus
{
    InProgress,
    Completed,
    Cancelled
}

public class OnboardingSession
{
    [Key]
    public int Id { get; set; }

    public int? TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public int? StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public Store? Store { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = OnboardingStatus.InProgress.ToString();

    [Required]
    [MaxLength(100)]
    public string CurrentStepKey { get; set; } = "comercio";

    [Required]
    public int CreatedByUsuarioId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ICollection<OnboardingStepState> Steps { get; set; } = new List<OnboardingStepState>();
}

public class OnboardingStepState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OnboardingSessionId { get; set; }

    [ForeignKey(nameof(OnboardingSessionId))]
    public OnboardingSession OnboardingSession { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string StepKey { get; set; } = string.Empty;

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedAt { get; set; }

    public string? DataJson { get; set; }
}

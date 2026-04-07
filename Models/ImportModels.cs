using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KodvianSuperMarket.Models;

public class ImportJob
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ImportType { get; set; } = string.Empty;

    public bool Upsert { get; set; }

    [Required]
    [MaxLength(20)]
    public string Mode { get; set; } = "Preview";

    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Completed";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ImportJobError> Errors { get; set; } = new List<ImportJobError>();
}

public class ImportJobError
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ImportJobId { get; set; }

    [ForeignKey(nameof(ImportJobId))]
    public ImportJob ImportJob { get; set; } = null!;

    public int RowNumber { get; set; }

    [Required]
    [MaxLength(100)]
    public string Field { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
}

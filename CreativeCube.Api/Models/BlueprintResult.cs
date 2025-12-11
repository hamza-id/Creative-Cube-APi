using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreativeCube.Api.Models;

public class BlueprintResult
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public long BlueprintId { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? ComplianceScore { get; set; } // Score %, e.g., 87.50

    [Column(TypeName = "jsonb")]
    public string? Violations { get; set; } // Array of violations from AI (stored as JSON string)

    [Column(TypeName = "jsonb")]
    public string? ExtractedData { get; set; } // Parsed OCR metadata or layout data (stored as JSON string)

    public string? ReportUrl { get; set; } // S3 / blob URL of generated PDF

    [Column(TypeName = "jsonb")]
    public string? AiRawResponse { get; set; } // Optional: raw response from AI for debugging (stored as JSON string)

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(BlueprintId))]
    public Blueprint? Blueprint { get; set; }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreativeCube.Api.Models;

public class Blueprint
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public long ProjectId { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty; // File storage path (S3, blob, etc.)

    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty; // deed / survey / architectural / mep / structural

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // uploaded / processing / completed

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }
}


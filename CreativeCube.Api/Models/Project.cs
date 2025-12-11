using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreativeCube.Api.Models;

public class Project
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ServiceType { get; set; } = string.Empty; // architectural / structural / mep

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,6)")]
    public decimal Latitude { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,6)")]
    public decimal Longitude { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // queued / processing / completed

    public long? AssignedTo { get; set; } // Engineer assigned to project

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }

    public ICollection<Blueprint> Blueprints { get; set; } = new List<Blueprint>();
}


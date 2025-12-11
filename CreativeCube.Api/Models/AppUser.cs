using System.ComponentModel.DataAnnotations;

namespace CreativeCube.Api.Models;

public class AppUser
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string IqamaNumber { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Mobile { get; set; }

    [MaxLength(200)]
    public string? OrganizationName { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


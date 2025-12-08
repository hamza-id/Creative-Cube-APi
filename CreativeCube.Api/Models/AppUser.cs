using System.ComponentModel.DataAnnotations;

namespace CreativeCube.Api.Models;

public class AppUser
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}


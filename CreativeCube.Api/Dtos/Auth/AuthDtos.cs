using System.ComponentModel.DataAnnotations;

namespace CreativeCube.Api.Dtos.Auth;

public record RegisterRequest(
    [property: Required] string FirstName,
    [property: Required] string LastName,
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password,
    [property: Required] string IqamaNumber,
    string? Mobile,
    string? OrganizationName,
    string? LicenseNumber);

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record RefreshTokenRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string RefreshToken);

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string IqamaNumber,
    string? Mobile,
    string? OrganizationName,
    string? LicenseNumber,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AuthResponse(
    UserDto User,
    string? AccessToken,
    DateTime? AccessTokenExpiresAt,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresAt);


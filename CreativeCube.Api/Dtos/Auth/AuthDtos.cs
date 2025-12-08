namespace CreativeCube.Api.Dtos.Auth;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string Email, string RefreshToken);

public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);


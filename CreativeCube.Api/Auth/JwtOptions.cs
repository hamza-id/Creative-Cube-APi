namespace CreativeCube.Api.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "CreativeCube";
    public string Audience { get; set; } = "CreativeCubeApi";
    // At least 32 chars for HS256 (256-bit)
    public string SecretKey { get; set; } = "u8yYgLr5m1pQ2sV9zC4xN7bD0fK3wR6tE8hJ1uP4sX7cV0mZ9qL2nB5aG8dR1kC3";

    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}


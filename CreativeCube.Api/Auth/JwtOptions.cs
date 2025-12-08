namespace CreativeCube.Api.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "CreativeCube";
    public string Audience { get; set; } = "CreativeCubeApi";
    public string SecretKey { get; set; } = "PLEASE_CHANGE_ME_TO_A_LONG_RANDOM_SECRET";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}


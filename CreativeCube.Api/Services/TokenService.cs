using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CreativeCube.Api.Auth;
using CreativeCube.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CreativeCube.Api.Services;

public class TokenService
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string token, DateTime expiresAt) GenerateAccessToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (_handler.WriteToken(token), expires);
    }

    public (string token, DateTime expiresAt) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var expires = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);
        return (token, expires);
    }
}


using CreativeCube.Api.Data;
using CreativeCube.Api.Dtos.Auth;
using CreativeCube.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CreativeCube.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> FindByEmailAsync(string email) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

    public async Task<(bool ok, string? error, AppUser? user)> RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
        {
            return (false, "Email already registered.", null);
        }

        var user = new AppUser
        {
            Email = request.Email
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return (true, null, user);
    }

    public async Task<(bool ok, AppUser? user)> ValidateCredentialsAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            return (false, null);
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        return (result == PasswordVerificationResult.Success, user);
    }

    public async Task SaveRefreshTokenAsync(AppUser user, string token, DateTime expiresAt)
    {
        user.RefreshToken = token;
        user.RefreshTokenExpiresAt = expiresAt;

        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public bool IsRefreshTokenValid(AppUser user, string token) =>
        user.RefreshToken == token && user.RefreshTokenExpiresAt >= DateTime.UtcNow;
}


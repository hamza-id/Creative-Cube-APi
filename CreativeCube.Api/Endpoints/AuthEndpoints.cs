using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using CreativeCube.Api.Dtos.Auth;
using CreativeCube.Api.Models;
using CreativeCube.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace CreativeCube.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, UserService users, TokenService tokens) =>
        {
            var (ok, error, user) = await users.RegisterAsync(request);
            if (!ok || user is null)
            {
                return Results.Conflict(new { message = error ?? "Registration failed." });
            }

            var (access, accessExp) = tokens.GenerateAccessToken(user);
            var (refresh, refreshExp) = tokens.GenerateRefreshToken();
            await users.SaveRefreshTokenAsync(user, refresh, refreshExp);

            return Results.Created($"/auth/profile", new AuthResponse(access, accessExp, refresh, refreshExp));
        })
        .WithTags("Auth")
        .WithOpenApi(op =>
        {
            op.Summary = "Register a new user";
            op.Description = "Creates a user and returns access/refresh tokens.";
            op.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString("{ \"email\": \"user@example.com\", \"password\": \"Pass123!\" }")
                    }
                }
            };
            return op;
        });

        group.MapPost("/login", async (LoginRequest request, UserService users, TokenService tokens) =>
        {
            var (ok, user) = await users.ValidateCredentialsAsync(request);
            if (!ok || user is null)
            {
                return Results.Unauthorized();
            }

            var (access, accessExp) = tokens.GenerateAccessToken(user);
            var (refresh, refreshExp) = tokens.GenerateRefreshToken();
            await users.SaveRefreshTokenAsync(user, refresh, refreshExp);

            return Results.Ok(new AuthResponse(access, accessExp, refresh, refreshExp));
        })
        .WithTags("Auth")
        .WithOpenApi(op =>
        {
            op.Summary = "Login";
            op.Description = "Validates credentials and returns access/refresh tokens.";
            op.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString("{ \"email\": \"user@example.com\", \"password\": \"Pass123!\" }")
                    }
                }
            };
            return op;
        });

        group.MapPost("/refresh-token", async (RefreshTokenRequest request, UserService users, TokenService tokens) =>
        {
            var user = await users.FindByEmailAsync(request.Email);
            if (user is null || !users.IsRefreshTokenValid(user, request.RefreshToken))
            {
                return Results.Unauthorized();
            }

            var (access, accessExp) = tokens.GenerateAccessToken(user);
            var (refresh, refreshExp) = tokens.GenerateRefreshToken();
            await users.SaveRefreshTokenAsync(user, refresh, refreshExp);

            return Results.Ok(new AuthResponse(access, accessExp, refresh, refreshExp));
        })
        .WithTags("Auth")
        .WithOpenApi(op =>
        {
            op.Summary = "Refresh access token";
            op.Description = "Exchanges a valid refresh token for new access/refresh tokens.";
            op.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString("{ \"email\": \"user@example.com\", \"refreshToken\": \"<refresh-token>\" }")
                    }
                }
            };
            return op;
        });

        group.MapGet("/profile", [Authorize] async (ClaimsPrincipal principal, UserService users) =>
        {
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.Unauthorized();
            }

            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { user.Id, user.Email });
        })
        .WithTags("Auth")
        .WithOpenApi(op =>
        {
            op.Summary = "Get current user profile";
            op.Description = "Requires Bearer token in the Authorization header.";
            return op;
        });

        return app;
    }
}


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
            var validation = ValidateRegister(request);
            if (validation is not null) return validation;

            var (ok, error, user) = await users.RegisterAsync(request);
            if (!ok || user is null)
            {
                return Results.Conflict(new { message = error ?? "Registration failed." });
            }

            var userDto = ToDto(user);
            var (access, accessExp) = tokens.GenerateAccessToken(user);
            var (refresh, refreshExp) = tokens.GenerateRefreshToken();
            await users.SaveRefreshTokenAsync(user, refresh, refreshExp);

            return Results.Created($"/auth/profile", new AuthResponse(userDto, access, accessExp, refresh, refreshExp));
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
                        Example = new OpenApiString("{ \"firstName\": \"John\", \"lastName\": \"Doe\", \"email\": \"user@example.com\", \"password\": \"Pass123!\", \"iqamaNumber\": \"1234567890\", \"mobile\": \"+15551234567\", \"organizationName\": \"Org Inc\", \"licenseNumber\": \"LIC-123\" }")
                    }
                }
            };
            return op;
        });

        group.MapPost("/login", async (LoginRequest request, UserService users, TokenService tokens) =>
        {
            var validation = ValidateLogin(request);
            if (validation is not null) return validation;

            var (ok, user) = await users.ValidateCredentialsAsync(request);
            if (!ok || user is null)
            {
                return Results.Unauthorized();
            }

            var userDto = ToDto(user);
            var (access, accessExp) = tokens.GenerateAccessToken(user);
            var (refresh, refreshExp) = tokens.GenerateRefreshToken();
            await users.SaveRefreshTokenAsync(user, refresh, refreshExp);

            return Results.Ok(new AuthResponse(userDto, access, accessExp, refresh, refreshExp));
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

            var userDto = ToDto(user);
            var (access, accessExp) = tokens.GenerateAccessToken(user);
            var (refresh, refreshExp) = tokens.GenerateRefreshToken();
            await users.SaveRefreshTokenAsync(user, refresh, refreshExp);

            return Results.Ok(new AuthResponse(userDto, access, accessExp, refresh, refreshExp));
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
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var email = principal?.FindFirst(ClaimTypes.Email)?.Value
                ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? principal?.FindFirstValue("email");
            
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.Unauthorized();
            }

            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                return Results.NotFound();
            }

            var userDto = ToDto(user);
            return Results.Ok(userDto);
        })
        .RequireAuthorization()
        .WithTags("Auth")
        .WithOpenApi(op =>
        {
            op.Summary = "Get current user profile";
            op.Description = "Requires Bearer token in the Authorization header.";
            return op;
        });

        return app;
    }

    private static IResult? ValidateRegister(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) ||
            string.IsNullOrWhiteSpace(req.LastName) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password) ||
            string.IsNullOrWhiteSpace(req.IqamaNumber))
        {
            return Results.BadRequest(new { message = "All fields (firstName, lastName, email, password, iqamaNumber) are required." });
        }
        return null;
    }

    private static IResult? ValidateLogin(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        {
            return Results.BadRequest(new { message = "Email and password are required." });
        }
        return null;
    }

    private static UserDto ToDto(AppUser user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.IqamaNumber, user.Mobile, user.OrganizationName, user.LicenseNumber, user.CreatedAt, user.UpdatedAt);
}


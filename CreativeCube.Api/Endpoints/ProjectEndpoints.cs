using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CreativeCube.Api.Data;
using CreativeCube.Api.Dtos.Project;
using CreativeCube.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace CreativeCube.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapPost("/", async (
            CreateProjectRequest request,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userIdClaim = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var project = new Project
            {
                UserId = userId,
                Name = request.Name,
                ServiceType = request.ServiceType,
                City = request.City,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Status = "queued"
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync();

            var response = ToResponse(project);
            return Results.Created($"/api/projects/{project.Id}", response);
        })
        .WithTags("2. Projects")
        .WithOpenApi(op =>
        {
            op.Summary = "Create a new project";
            op.Description = "Creates a new project for the authenticated user.\n\n" +
                           "**Request Body Required:**\n" +
                           "- `name`: Project name/title (required)\n" +
                           "- `serviceType`: Type of service - one of: `architectural`, `structural`, `mep` (required)\n" +
                           "- `city`: City name where project is located (required)\n" +
                           "- `latitude`: GPS latitude coordinate (required, decimal)\n" +
                           "- `longitude`: GPS longitude coordinate (required, decimal)";
            op.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString("{\"name\": \"Residential Building\", \"serviceType\": \"architectural\", \"city\": \"Riyadh\", \"latitude\": 24.7136, \"longitude\": 46.6753}")
                    }
                }
            };
            return op;
        });

        group.MapGet("/", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userIdClaim = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var projects = await db.Projects
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Blueprints)
                .ToListAsync();

            var responses = projects.Select(p => ToResponse(p, includeBlueprints: true)).ToList();

            return Results.Ok(responses);
        })
        .WithTags("2. Projects")
        .WithOpenApi(op =>
        {
            op.Summary = "Get all projects";
            op.Description = "Returns a list of all projects created by the authenticated user.\n\n" +
                           "**Authentication Required:**\n" +
                           "- Bearer token must be provided in the Authorization header\n" +
                           "- Returns projects ordered by creation date (newest first)";
            return op;
        });

        group.MapGet("/{id}", async (long id, ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userIdClaim = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .Include(p => p.Blueprints)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project == null)
            {
                return Results.NotFound();
            }

            var response = ToResponse(project, includeBlueprints: true);
            return Results.Ok(response);
        })
        .WithTags("2. Projects")
        .WithOpenApi(op =>
        {
            op.Summary = "Get project by ID";
            op.Description = "Returns a specific project by its ID, including associated blueprints.\n\n" +
                           "**Path Parameters:**\n" +
                           "- `id`: Project ID (required)\n\n" +
                           "**Authentication Required:**\n" +
                           "- Bearer token must be provided in the Authorization header\n" +
                           "- Project must belong to the authenticated user";
            return op;
        });

        group.MapPost("/{id}/assign", async (
            long id,
            AssignProjectRequest request,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userIdClaim = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project == null)
            {
                return Results.NotFound();
            }

            project.AssignedTo = request.AssignedTo;
            project.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var response = ToResponse(project);
            return Results.Ok(response);
        })
        .WithTags("2. Projects")
        .WithOpenApi(op =>
        {
            op.Summary = "Assign project to engineer";
            op.Description = "Assigns a project to an engineer by their user ID.\n\n" +
                           "**Path Parameters:**\n" +
                           "- `id`: Project ID (required)\n\n" +
                           "**Request Body Required:**\n" +
                           "- `assignedTo`: Engineer's user ID (Guid) to assign the project to (required)\n\n" +
                           "**Authentication Required:**\n" +
                           "- Bearer token must be provided in the Authorization header\n" +
                           "- Project must belong to the authenticated user";
            op.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new OpenApiString("{\"assignedTo\": \"a1b2c3d4-e5f6-7890-1234-567890abcdef\"}")
                    }
                }
            };
            return op;
        });

        return app;
    }

    private static ProjectResponse ToResponse(Project project, bool includeBlueprints = false)
    {
        var blueprints = includeBlueprints && project.Blueprints != null
            ? project.Blueprints.Select(b => new BlueprintResponse(
                b.Id,
                b.ProjectId,
                b.FilePath,
                b.FileType,
                b.Status,
                b.CreatedAt,
                b.UpdatedAt)).ToList()
            : null;

        return new ProjectResponse(
            project.Id,
            project.UserId,
            project.Name,
            project.ServiceType,
            project.City,
            project.Latitude,
            project.Longitude,
            project.Status,
            project.AssignedTo,
            project.CreatedAt,
            project.UpdatedAt,
            blueprints);
    }
}


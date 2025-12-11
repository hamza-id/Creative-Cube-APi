using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CreativeCube.Api.Data;
using CreativeCube.Api.Dtos.Blueprint;
using CreativeCube.Api.Models;
using CreativeCube.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace CreativeCube.Api.Endpoints;

public static class BlueprintEndpoints
{
    public static IEndpointRouteBuilder MapBlueprintEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blueprint").RequireAuthorization();

        group.MapPost("/upload", async (
            HttpRequest request,
            ClaimsPrincipal principal,
            AppDbContext db,
            OssService ossService) =>
        {
            var userIdClaim = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            // Check if file is uploaded
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                return Results.BadRequest(new { message = "No file uploaded" });
            }

            // Get form data
            if (!long.TryParse(request.Form["projectId"].ToString(), out var projectId))
            {
                return Results.BadRequest(new { message = "projectId is required" });
            }

            var fileType = request.Form["fileType"].ToString();
            if (string.IsNullOrWhiteSpace(fileType))
            {
                return Results.BadRequest(new { message = "fileType is required (deed / survey / architectural / mep / structural)" });
            }

            // Verify project belongs to user
            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
            
            if (project == null)
            {
                return Results.NotFound(new { message = "Project not found" });
            }

            var file = request.Form.Files[0];
            if (file.Length == 0)
            {
                return Results.BadRequest(new { message = "File is empty" });
            }

            // Upload to OSS
            string filePath;
            try
            {
                using var stream = file.OpenReadStream();
                filePath = await ossService.UploadFileAsync(stream, file.FileName, "blueprints");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"Failed to upload file: {ex.Message}" });
            }

            // Create blueprint record
            var blueprint = new Blueprint
            {
                ProjectId = projectId,
                FilePath = filePath,
                FileType = fileType,
                Status = "uploaded"
            };

            db.Blueprints.Add(blueprint);
            await db.SaveChangesAsync();

            var response = new UploadBlueprintResponse(
                blueprint.Id,
                blueprint.ProjectId,
                blueprint.FilePath,
                blueprint.FileType,
                blueprint.Status,
                blueprint.CreatedAt);

            return Results.Created($"/api/blueprint/{blueprint.Id}", response);
        })
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .WithTags("3. Blueprints")
        .WithOpenApi(op =>
        {
            op.Summary = "Upload blueprint file";
            op.Description = "Uploads a blueprint file to OSS and creates a blueprint record.\n\n" +
                           "**Form Data Required:**\n" +
                           "- `file`: The blueprint file to upload (PDF, DWG, DXF, JPG, PNG, TIFF)\n" +
                           "- `projectId`: The ID of the project this blueprint belongs to\n" +
                           "- `fileType`: Type of blueprint - one of: `deed`, `survey`, `architectural`, `mep`, `structural`";
            return op;
        });

        group.MapPost("/{id}/process", async (
            long id,
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

            var blueprint = await db.Blueprints
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (blueprint == null || blueprint.Project?.UserId != userId)
            {
                return Results.NotFound(new { message = "Blueprint not found" });
            }

            // Update status to processing
            blueprint.Status = "processing";
            blueprint.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // TODO: Trigger AI processing here (call external AI service)
            // For now, we'll just update the status
            // In production, this would trigger an async job/queue

            var response = new ProcessBlueprintResponse(
                blueprint.Id,
                blueprint.Status,
                "Blueprint processing started");

            return Results.Ok(response);
        })
        .WithTags("3. Blueprints")
        .WithOpenApi(op =>
        {
            op.Summary = "Process blueprint";
            op.Description = "Triggers AI processing for a blueprint to analyze compliance and extract data.\n\n" +
                           "**Path Parameters:**\n" +
                           "- `id`: Blueprint ID (required)\n\n" +
                           "**Authentication Required:**\n" +
                           "- Bearer token must be provided in the Authorization header\n" +
                           "- Blueprint must belong to a project owned by the authenticated user\n\n" +
                           "**Note:** This endpoint updates the blueprint status to 'processing' and triggers an async AI analysis job.";
            return op;
        });

        group.MapGet("/{id}/result", async (
            long id,
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

            var blueprint = await db.Blueprints
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (blueprint == null || blueprint.Project?.UserId != userId)
            {
                return Results.NotFound(new { message = "Blueprint not found" });
            }

            var result = await db.BlueprintResults
                .FirstOrDefaultAsync(r => r.BlueprintId == id);

            if (result == null)
            {
                return Results.NotFound(new { message = "No result found for this blueprint" });
            }

            // Parse JSON strings to objects
            var violations = !string.IsNullOrEmpty(result.Violations)
                ? JsonSerializer.Deserialize<object>(result.Violations)
                : null;
            
            var extractedData = !string.IsNullOrEmpty(result.ExtractedData)
                ? JsonSerializer.Deserialize<object>(result.ExtractedData)
                : null;
            
            var aiRawResponse = !string.IsNullOrEmpty(result.AiRawResponse)
                ? JsonSerializer.Deserialize<object>(result.AiRawResponse)
                : null;

            var response = new BlueprintResultResponse(
                result.Id,
                result.BlueprintId,
                result.ComplianceScore,
                violations,
                extractedData,
                result.ReportUrl,
                aiRawResponse,
                result.CreatedAt,
                result.UpdatedAt);

            return Results.Ok(response);
        })
        .WithTags("3. Blueprints")
        .WithOpenApi(op =>
        {
            op.Summary = "Get blueprint result";
            op.Description = "Returns the AI processing results for a blueprint, including compliance score, violations, and extracted data.\n\n" +
                           "**Path Parameters:**\n" +
                           "- `id`: Blueprint ID (required)\n\n" +
                           "**Authentication Required:**\n" +
                           "- Bearer token must be provided in the Authorization header\n" +
                           "- Blueprint must belong to a project owned by the authenticated user\n\n" +
                           "**Response Includes:**\n" +
                           "- `complianceScore`: Compliance percentage score\n" +
                           "- `violations`: Array of detected violations\n" +
                           "- `extractedData`: Parsed OCR metadata and layout data\n" +
                           "- `reportUrl`: URL to generated PDF report (if available)";
            return op;
        });

        group.MapPost("/{id}/report", async (
            long id,
            ClaimsPrincipal principal,
            AppDbContext db,
            OssService ossService) =>
        {
            var userIdClaim = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("sub");
            
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var blueprint = await db.Blueprints
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (blueprint == null || blueprint.Project?.UserId != userId)
            {
                return Results.NotFound(new { message = "Blueprint not found" });
            }

            var result = await db.BlueprintResults
                .FirstOrDefaultAsync(r => r.BlueprintId == id);

            if (result == null)
            {
                return Results.NotFound(new { message = "No result found. Process the blueprint first." });
            }

            // TODO: Generate PDF report and upload to OSS
            // For now, we'll create a placeholder report URL
            // In production, this would generate an actual PDF report
            var reportFileName = $"report_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            string reportUrl;
            
            try
            {
                // Generate report content (placeholder - replace with actual PDF generation)
                var reportContent = GenerateReportContent(result);
                using var reportStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reportContent));
                reportUrl = await ossService.UploadFileAsync(reportStream, reportFileName, "reports");
                
                // Update result with report URL
                result.ReportUrl = reportUrl;
                result.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"Failed to generate report: {ex.Message}" });
            }

            var response = new GenerateReportResponse(
                reportUrl,
                "Report generated successfully");

            return Results.Ok(response);
        })
        .WithTags("3. Blueprints")
        .WithOpenApi(op =>
        {
            op.Summary = "Generate blueprint report";
            op.Description = "Generates a PDF report for the blueprint analysis results and uploads it to OSS.\n\n" +
                           "**Path Parameters:**\n" +
                           "- `id`: Blueprint ID (required)\n\n" +
                           "**Authentication Required:**\n" +
                           "- Bearer token must be provided in the Authorization header\n" +
                           "- Blueprint must belong to a project owned by the authenticated user\n" +
                           "- Blueprint must have processing results available\n\n" +
                           "**Response:**\n" +
                           "- Returns the OSS URL of the generated PDF report";
            return op;
        });

        return app;
    }

    private static string GenerateReportContent(BlueprintResult result)
    {
        // Placeholder report content - replace with actual PDF generation library
        return $"Blueprint Analysis Report\n" +
               $"Compliance Score: {result.ComplianceScore}%\n" +
               $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n";
    }
}


using System.ComponentModel.DataAnnotations;

namespace CreativeCube.Api.Dtos.Blueprint;

public record UploadBlueprintRequest(
    [property: Required] long ProjectId,
    [property: Required, MaxLength(50)] string FileType); // deed / survey / architectural / mep / structural

public record UploadBlueprintResponse(
    long Id,
    long ProjectId,
    string FilePath,
    string FileType,
    string Status,
    DateTime CreatedAt);

public record ProcessBlueprintResponse(
    long BlueprintId,
    string Status,
    string Message);

public record BlueprintResultResponse(
    long Id,
    long BlueprintId,
    decimal? ComplianceScore,
    object? Violations, // JSON object
    object? ExtractedData, // JSON object
    string? ReportUrl,
    object? AiRawResponse, // JSON object
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record GenerateReportResponse(
    string ReportUrl,
    string Message);


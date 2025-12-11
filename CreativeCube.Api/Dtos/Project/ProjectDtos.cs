using System.ComponentModel.DataAnnotations;

namespace CreativeCube.Api.Dtos.Project;

public record CreateProjectRequest(
    [property: Required, MaxLength(255)] string Name,
    [property: Required, MaxLength(50)] string ServiceType, // architectural / structural / mep
    [property: Required, MaxLength(100)] string City,
    [property: Required] decimal Latitude,
    [property: Required] decimal Longitude);

public record AssignProjectRequest(
    [property: Required] long AssignedTo);

public record ProjectResponse(
    long Id,
    Guid UserId,
    string Name,
    string ServiceType,
    string City,
    decimal Latitude,
    decimal Longitude,
    string Status,
    long? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<BlueprintResponse>? Blueprints = null);

public record BlueprintResponse(
    long Id,
    long ProjectId,
    string FilePath,
    string FileType,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);


namespace WorkflowService.Models.Dto;

/// <summary>JSON-serializable DTO for caching GetClientAttributesResponse.</summary>
internal sealed record ClientAttributesCacheDto(
    string? Name,
    string? Username,
    string? Phone,
    string? Email,
    Dictionary<string, string> CustomAttributes);

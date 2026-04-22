namespace WorkflowService.Models.Dto;

internal sealed record ClientAttributesCacheDto(
    string? Name,
    string? Username,
    string? Phone,
    string? Email,
    Dictionary<string, string> CustomAttributes,
    string? ClientChannelId = null);

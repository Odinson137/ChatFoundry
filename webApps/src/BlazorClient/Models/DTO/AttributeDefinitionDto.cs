using BlazorClient.Interfaces;

namespace BlazorClient.Models.DTO;

public class AttributeDefinitionDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = "String";
}

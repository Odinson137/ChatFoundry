namespace ClientService.GraphQL.Dtos;

public class AttributeDefinitionDto
{
    public string Key { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
}

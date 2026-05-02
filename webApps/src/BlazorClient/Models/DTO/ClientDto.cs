namespace BlazorClient.Models.DTO;

public class ClientDto
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public List<ClientChannelDto> ClientChannels { get; set; } = [];
}

public class ClientChannelDto
{
    public Guid Id { get; set; }
    public Guid? ChannelId { get; set; }
    public string Channel { get; set; } = "";
    public string ExternalUserId { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ClientAttributeDto> Attributes { get; set; } = [];
}

public class ClientAttributeDto
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public record SetClientChannelAttributesRequest(
    Guid ClientChannelId,
    string? Name,
    string? LastName,
    string? Username,
    string? Phone,
    string? Email,
    List<ClientAttributeDto>? CustomAttributes);

public class ClientsPageResult
{
    public List<ClientDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? EndCursor { get; set; }
    public string? StartCursor { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string? Payload { get; set; }
    public string Direction { get; set; } = "";
    public string MessageKind { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class MessagesPageResult
{
    public List<MessageDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public string? EndCursor { get; set; }
}

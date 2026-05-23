namespace Shared.Infrastructure.Options;

public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "unknown";
    public string ServiceVersion { get; set; } = "1.0.0";
    public SeqOptions Seq { get; set; } = new();
    public JaegerOptions Jaeger { get; set; } = new();
}

public class SeqOptions
{
    public string Url { get; set; } = "http://seq:5341";
    public string ApiKey { get; set; } = "";
}

public class JaegerOptions
{
    public string Endpoint { get; set; } = "http://jaeger:4317";
}

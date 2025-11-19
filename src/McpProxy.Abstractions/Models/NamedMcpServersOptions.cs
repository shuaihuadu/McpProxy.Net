namespace McpProxy;

public class NamedMcpServersOptions
{
    [JsonPropertyName("servers")]
    public Dictionary<string, NamedMcpServerOptions> Servers { get; set; }
}

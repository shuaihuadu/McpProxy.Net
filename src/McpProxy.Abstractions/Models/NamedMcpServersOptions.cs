namespace McpProxy;

/// <summary>
/// 命名的MCP服务器集合的配置选项
/// </summary>
public class NamedMcpServersOptions
{
    /// <summary>
    /// 获取mcp server的配置节名称
    /// </summary>
    public const string SectionName = "servers";

    /// <summary>
    /// 获取或设置服务器配置的字典，以服务器名称为键
    /// </summary>
    [JsonPropertyName("servers")]
    public Dictionary<string, NamedMcpServerInfo>? Servers { get; set; }
}

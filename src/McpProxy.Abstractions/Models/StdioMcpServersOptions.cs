namespace McpProxy;

/// <summary>
/// stdio传输类型的MCP服务器集合配置选项
/// 包含从配置文件（如mcp.json）加载的服务器定义
/// </summary>
public class StdioMcpServersOptions
{
    /// <summary>
    /// 获取mcp server的配置节名称
    /// </summary>
    public const string SectionName = "servers";

    /// <summary>
    /// 获取或设置服务器配置的字典，以服务器名称为键
    /// </summary>
    [JsonPropertyName("servers")]
    public Dictionary<string, StdioMcpServer>? Servers { get; set; }
}

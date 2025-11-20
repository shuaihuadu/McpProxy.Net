namespace McpProxy;

/// <summary>
/// Contains configuration information for an MCP server defined in the mcp.json.
/// </summary>
public class NamedMcpServerInfo
{
    const int TimeoutDefault = 60;

    /// <summary>
    /// Gets or sets the name of the server, typically derived from the key in the configuration file.
    /// This property is not serialized to/from JSON.
    /// </summary>
    [JsonIgnore]
    public string? Name { get; set; }

    /// <summary>
    /// Gets a description of the server's purpose or capabilities.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the user-friendly title for the server.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>
    /// 获取或设置服务器是否被禁用
    /// 如果为 true，服务器将被禁用且不会启动
    /// </summary>
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }

    /// <summary>
    /// 获取或设置服务器类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置超时时间（秒）
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = TimeoutDefault;

    /// <summary>
    /// 获取或设置运行服务器的命令
    /// </summary>
    [JsonPropertyName("command")]
    [JsonRequired]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置传递给服务器的命令行参数列表
    /// </summary>
    [JsonPropertyName("args")]
    public IList<string>? Args { get; set; }

    /// <summary>
    /// 获取或设置传递给服务器的环境变量
    /// </summary>
    [JsonPropertyName("env")]
    public IDictionary<string, string?>? Env { get; set; }

    /// <summary>
    /// 获取或设置工作目录
    /// </summary>
    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }
}

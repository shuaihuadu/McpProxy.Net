namespace McpProxy;

/// <summary>
/// 服务器配置基类
/// </summary>
public class NamedMcpServerOptions
{
    const int TimeoutDefault = 60;

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
    public int TimeoutSeconds { get; set; } = TimeoutDefault;

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
    public IList<string>? Arguments { get; set; }

    /// <summary>
    /// 获取或设置传递给服务器的环境变量
    /// </summary>
    [JsonPropertyName("env")]
    public IDictionary<string, string?>? EnvironmentVariables { get; }

    /// <summary>
    /// 获取或设置工作目录
    /// </summary>
    [JsonPropertyName("cwd")]
    public string? WorkingDirectory { get; set; }


    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    public StdioClientTransportOptions? ToStdioClientTransportOptions(string name)
    {
        if (this.Type == "stdio")
        {
            return new StdioClientTransportOptions
            {
                Name = name,
                Command = this.Command,
                Arguments = this.Arguments,
                EnvironmentVariables = this.EnvironmentVariables,
                WorkingDirectory = this.WorkingDirectory,
                ShutdownTimeout = TimeSpan.FromSeconds(this.TimeoutSeconds),
                // TODO StandardErrorLines
            };
        }

        return default;
    }
}

// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy;

/// <summary>
/// 包含在mcp.json中定义的MCP服务器的配置信息
/// </summary>
public class StdioMcpServer
{
    /// <summary>
    /// 默认超时时间（秒）
    /// </summary>
    private const int TimeoutDefault = 60;

    /// <summary>
    /// 获取或设置服务器的名称，通常派生自配置文件中的键
    /// 此属性不会序列化到JSON或从JSON反序列化
    /// </summary>
    [JsonIgnore]
    public string? Name { get; set; }

    /// <summary>
    /// 获取或设置服务器的用途或功能的描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置服务器的用户友好标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 获取或设置服务器是否启用
    /// 如果为true（默认值），服务器将被启用并可以启动；如果为false，服务器将被禁用且不会启动
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置服务器传输类型（例如："stdio"）
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "stdio";

    /// <summary>
    /// 获取或设置超时时间（秒），默认为60秒
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = TimeoutDefault;

    /// <summary>
    /// 获取或设置运行服务器的命令（必需）
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
    /// 获取或设置传递给服务器的环境变量字典
    /// </summary>
    [JsonPropertyName("env")]
    public IDictionary<string, string?>? Env { get; set; }

    /// <summary>
    /// 获取或设置服务器进程的工作目录
    /// </summary>
    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }
}

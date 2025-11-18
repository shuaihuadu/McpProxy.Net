using System.Text.Json.Serialization;

namespace McpProxy;

/// <summary>
/// 服务器配置基类
/// </summary>
public abstract class McpServerOptions
{
    const int TimeoutDefault = 60;

    /// <summary>
    /// TODO Mcp Server Name
    /// </summary>
    public string Name { get; set; }

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
    public abstract string Type { get; }

    /// <summary>
    /// 获取或设置超时时间（秒）
    /// </summary>
    [JsonPropertyName("timeout")]
    public int TimeoutSeconds { get; set; } = TimeoutDefault;
}

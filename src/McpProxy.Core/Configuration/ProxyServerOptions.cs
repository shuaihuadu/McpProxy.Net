using System.ComponentModel.DataAnnotations;

namespace McpProxy.Core.Configuration;

/// <summary>
/// MCP代理服务器的配置选项
/// </summary>
public sealed class ProxyServerOptions
{
    /// <summary>
    /// 获取或设置代理的运行模式
    /// </summary>
    [Required(ErrorMessage = "Mode is required")]
    public required ProxyMode Mode { get; set; }

    /// <summary>
    /// 获取或设置MCP服务器列表（在StdioToHttp模式下使用）
    /// </summary>
    public List<McpServerConfig>? McpServers { get; set; }

    /// <summary>
    /// 获取或设置SSE客户端配置（在SseToStdio模式下使用）
    /// </summary>
    public SseClientOptions? SseClient { get; set; }

    /// <summary>
    /// 获取或设置HTTP服务器配置（在StdioToHttp模式下使用）
    /// </summary>
    public HttpServerOptions? HttpServer { get; set; }

    /// <summary>
    /// 获取或设置是否在多服务器时使用命名空间前缀（默认值：true）
    /// </summary>
    public bool UseNamespacePrefix { get; set; } = true;

    /// <summary>
    /// 获取或设置是否允许按服务器名称过滤（默认值：true）
    /// </summary>
    public bool AllowServerFilter { get; set; } = true;

    /// <summary>
    /// 获取或设置服务器断开时是否自动重连（默认值：true）
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// 获取或设置健康检查间隔（秒，默认值：30）
    /// </summary>
    [Range(5, 600, ErrorMessage = "HealthCheckInterval must be between 5 and 600 seconds")]
    public int HealthCheckInterval { get; set; } = 30;
}

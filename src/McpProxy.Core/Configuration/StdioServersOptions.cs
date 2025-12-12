using System.ComponentModel.DataAnnotations;

namespace McpProxy.Core.Configuration;

/// <summary>
/// Stdio服务器配置选项
/// 用于配置一个或多个本地Stdio MCP服务器
/// </summary>
public sealed class StdioServersOptions
{
    /// <summary>
    /// 获取或设置服务器列表
    /// </summary>
    [Required(ErrorMessage = "At least one server is required")]
    [MinLength(1, ErrorMessage = "At least one server is required")]
    public required List<McpServerConfig> Servers { get; set; }

    /// <summary>
    /// 获取或设置是否在聚合工具列表时使用命名空间前缀（默认值：true）
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

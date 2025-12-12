using System.ComponentModel.DataAnnotations;

namespace McpProxy.Core.Configuration;

/// <summary>
/// 托管代理的HTTP服务器配置
/// </summary>
public sealed class HttpServerOptions
{
    /// <summary>
    /// 获取或设置要绑定的主机地址（默认值：localhost）
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 获取或设置要监听的端口号（默认值：3000）
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 3000;

    /// <summary>
    /// 获取或设置服务器是否应该是无状态的
    /// </summary>
    public bool Stateless { get; set; } = false;

    /// <summary>
    /// 获取或设置允许的CORS源列表
    /// </summary>
    public List<string>? AllowedOrigins { get; set; }
}

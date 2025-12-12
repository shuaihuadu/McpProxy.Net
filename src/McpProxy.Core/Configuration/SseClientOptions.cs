using System.ComponentModel.DataAnnotations;

namespace McpProxy.Core.Configuration;

/// <summary>
/// 连接到基于SSE的MCP服务器的配置
/// </summary>
public sealed class SseClientOptions
{
    /// <summary>
    /// 获取或设置SSE服务器的URL地址
    /// </summary>
    [Required(ErrorMessage = "Url is required")]
    [Url(ErrorMessage = "Url must be a valid URL")]
    public required string Url { get; set; }

    /// <summary>
    /// 获取或设置要在请求中包含的HTTP头
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// 获取或设置是否验证SSL证书
    /// </summary>
    public bool VerifySsl { get; set; } = true;

    /// <summary>
    /// 获取或设置用于Bearer Token认证的访问令牌
    /// 也可以通过环境变量 API_ACCESS_TOKEN 设置
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// 获取或设置OAuth2客户端凭据认证配置
    /// </summary>
    public OAuth2ClientCredentialsOptions? OAuth2 { get; set; }
}

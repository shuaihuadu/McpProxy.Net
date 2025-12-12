using System.ComponentModel.DataAnnotations;

namespace McpProxy.Core.Configuration;

/// <summary>
/// OAuth2客户端凭据流（Client Credentials Flow）的配置
/// </summary>
public sealed class OAuth2ClientCredentialsOptions
{
    /// <summary>
    /// 获取或设置OAuth2客户端ID
    /// </summary>
    [Required(ErrorMessage = "ClientId is required for OAuth2")]
    public required string ClientId { get; set; }

    /// <summary>
    /// 获取或设置OAuth2客户端密钥
    /// </summary>
    [Required(ErrorMessage = "ClientSecret is required for OAuth2")]
    public required string ClientSecret { get; set; }

    /// <summary>
    /// 获取或设置OAuth2令牌端点URL
    /// </summary>
    [Required(ErrorMessage = "TokenUrl is required for OAuth2")]
    [Url(ErrorMessage = "TokenUrl must be a valid URL")]
    public required string TokenUrl { get; set; }

    /// <summary>
    /// 获取或设置OAuth2请求的作用域（可选）
    /// </summary>
    public string? Scope { get; set; }
}

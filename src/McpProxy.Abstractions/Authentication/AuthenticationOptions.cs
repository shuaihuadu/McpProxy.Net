namespace McpProxy;

/// <summary>
/// 认证配置
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// 获取或设置认证类型
    /// </summary>
    public AuthenticationType Type { get; set; } = AuthenticationType.None;

    /// <summary>
    /// 获取或设置Bearer Token
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// 获取或设置Basic认证用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 获取或设置Basic认证密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 获取或设置OAuth2客户端ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 获取或设置OAuth2客户端密钥
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// 获取或设置OAuth2令牌URL
    /// </summary>
    public string? TokenUrl { get; set; }

    /// <summary>
    /// 获取或设置OAuth2作用域
    /// </summary>
    public string[]? Scopes { get; set; }

    /// <summary>
    /// 获取或设置访问令牌（用于OAuth2，在获取后存储）
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// 获取或设置令牌过期时间
    /// </summary>
    public DateTime? TokenExpiry { get; set; }

    /// <summary>
    /// API Key
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// API Key参数名
    /// </summary>
    public string? ApiKeyName { get; set; }

    /// <summary>
    /// API Key位置（header, query）
    /// </summary>
    public ApiKeyLocation? ApiKeyLocation { get; set; }
}

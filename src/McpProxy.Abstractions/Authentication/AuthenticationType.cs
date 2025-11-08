namespace McpProxy;

/// <summary>
/// 认证类型
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// 无认证
    /// </summary>
    None,
    /// <summary>
    /// Bearer Token认证
    /// </summary>
    Bearer,
    /// <summary>
    /// Basic认证
    /// </summary>
    Basic,
    /// <summary>
    /// OAuth2客户端凭证认证
    /// </summary>
    OAuth2ClientCredentials,
    /// <summary>
    /// API KEY
    /// </summary>
    ApiKey
}

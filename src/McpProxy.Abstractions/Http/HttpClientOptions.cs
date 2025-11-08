namespace McpProxy;

/// <summary>
/// HTTP客户端配置
/// </summary>
public class HttpClientOptions
{
    /// <summary>
    /// 默认的请求超时时间（秒）
    /// </summary>
    public const int DEFAULT_TIMEOUT_SECONDS = 30;

    /// <summary>
    /// 获取或设置请求中的选头信息
    /// </summary>
    public Dictionary<string, string?>? Headers { get; set; }

    /// <summary>
    /// 获取或设置身份认证头信息
    /// </summary>
    public AuthenticationHeaderValue? AuthenticationHeader { get; set; }

    /// <summary>
    /// 获取或设置请求超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = DEFAULT_TIMEOUT_SECONDS;

    /// <summary>
    /// 获取或设置是否进行SSL验证
    /// </summary>
    public bool? VerifySsl { get; set; }
}

using System.Net.Http.Headers;

namespace McpProxy;

/// <summary>
/// 认证提供者接口
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// 获取认证头信息
    /// </summary>
    /// <returns>认证头信息</returns>
    Task<AuthenticationHeaderValue?> GetAuthenticationHeaderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
}

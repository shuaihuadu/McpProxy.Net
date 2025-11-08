namespace McpProxy;

/// <summary>
/// MCPProxy 的 HttpClient 提供者
/// </summary>
public interface IHttpClientProvider
{
    /// <summary>
    /// 获取HttpClient
    /// </summary>
    /// <param name="clientName">客户端名称</param>
    /// <param name="httpClientConfiguration">HTTP客户端配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置的HttpClient实例</returns>
    Task<HttpClient> GetHttpClientAsync(string clientName, HttpClientOptions httpClientConfiguration, CancellationToken cancellationToken = default);
}

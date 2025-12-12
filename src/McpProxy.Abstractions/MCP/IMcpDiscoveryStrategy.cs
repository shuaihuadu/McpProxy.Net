namespace McpProxy;

/// <summary>
/// 定义MCP服务器发现策略的接口
/// </summary>
public interface IMcpServerDiscoveryStrategy : IAsyncDisposable
{
    /// <summary>
    /// 通过此策略发现可用的MCP服务器
    /// </summary>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>已发现的MCP服务器集合</returns>
    Task<IEnumerable<IMcpServerProvider>> DiscoverServersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 根据名称查找服务器提供者
    /// </summary>
    /// <param name="name">要查找的服务器名称</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>如果找到则返回服务器提供者</returns>
    /// <exception cref="KeyNotFoundException">当未找到指定名称的服务器时抛出</exception>
    Task<IMcpServerProvider> FindServerProviderAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// 获取指定服务器的MCP客户端
    /// </summary>
    /// <param name="name">要获取客户端的服务器名称</param>
    /// <param name="clientOptions">可选的客户端配置选项。如果为null，则使用默认选项</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>可与指定服务器通信的MCP客户端</returns>
    /// <exception cref="KeyNotFoundException">当未找到指定名称的服务器时抛出</exception>
    /// <exception cref="ArgumentNullException">当name参数为null时抛出</exception>
    Task<McpClient> GetOrCreateClientAsync(string name, McpClientOptions? clientOptions = null, CancellationToken cancellationToken = default);
}

namespace McpProxy;

/// <summary>
/// MCP服务器发现策略的基类，提供通用功能
/// 实现客户端缓存和按名称查找服务器提供者的功能
/// </summary>
/// <param name="logger">日志记录器实例</param>
public abstract class BaseDiscoveryStrategy(ILogger logger) : IMcpServerDiscoveryStrategy
{
    /// <summary>
    /// 获取此发现策略的日志记录器实例
    /// </summary>
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 获取由此发现策略创建的MCP客户端缓存，以服务器名称为键（不区分大小写）
    /// </summary>
    protected readonly Dictionary<string, McpClient> _clientCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 标识对象是否已被释放
    /// </summary>
    private bool _disposed = false;

    /// <inheritdoc/>
    public abstract Task<IEnumerable<IMcpServerProvider>> DiscoverServersAsync(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public async Task<IMcpServerProvider> FindServerProviderAsync(string name, CancellationToken cancellationToken)
    {
        // 验证参数不为null
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        // 验证名称不为空或空白字符串
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Server name cannot be null or empty.", nameof(name));
        }

        // 发现所有服务器
        IEnumerable<IMcpServerProvider> serverProviders = await this.DiscoverServersAsync(cancellationToken).ConfigureAwait(false);

        // 查找匹配名称的服务器（不区分大小写）
        foreach (IMcpServerProvider serverProvider in serverProviders)
        {
            McpServerMetadata metadata = serverProvider.CreateMetadata();

            if (metadata.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return serverProvider;
            }
        }

        // 未找到匹配的服务器，抛出异常
        throw new KeyNotFoundException($"No MCP server found with the name '{name}'.");
    }

    /// <inheritdoc/>
    public async Task<McpClient> GetOrCreateClientAsync(string name, McpClientOptions? clientOptions = null, CancellationToken cancellationToken = default)
    {
        // 验证参数不为null
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        // 验证名称不为空或空白字符串
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Server name cannot be null or empty.", nameof(name));
        }

        // 尝试从缓存中获取客户端
        if (this._clientCache.TryGetValue(name, out McpClient? client))
        {
            this._logger.LogDebug("Using cached MCP client for server '{ServerName}'.", name);
            return client;
        }

        // 查找服务器提供者
        IMcpServerProvider serverProvider = await this.FindServerProviderAsync(name, cancellationToken).ConfigureAwait(false);

        // 创建新的客户端
        this._logger.LogInformation("Creating new MCP client for server '{ServerName}'.", name);
        client = await serverProvider.CreateClientAsync(clientOptions ?? new McpClientOptions(), cancellationToken).ConfigureAwait(false);

        // 缓存客户端
        this._clientCache[name] = client;

        return client;
    }

    /// <summary>
    /// 释放所有缓存的MCP客户端，并提供双重释放保护
    /// </summary>
    /// <returns>表示异步释放操作的任务</returns>
    public async ValueTask DisposeAsync()
    {
        // 防止重复释放
        if (this._disposed)
        {
            return;
        }

        try
        {
            // 首先，让派生类释放其资源（与基类清理隔离）
            try
            {
                await this.DisposeAsyncCore().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred while disposing derived resources in discovery strategy {StrategyType}", this.GetType().Name);
            }

            // 然后使用最佳努力方法释放我们自己的关键资源
            Task[] clientDisposalTasks = this._clientCache.Values.Select(async client =>
            {
                try
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Failed to dispose MCP client in discovery strategy {StrategyType}", this.GetType().Name);
                }
            }).ToArray();

            await Task.WhenAll(clientDisposalTasks).ConfigureAwait(false);
            this._clientCache.Clear();
        }
        catch (Exception ex)
        {
            // 记录释放失败但不抛出异常 - 确保清理过程继续进行
            // 个别的释放错误不应阻止整体释放过程
            this._logger.LogError(ex, "Error occurred while disposing discovery strategy {StrategyType}. Some resources may not have been properly disposed.", this.GetType().Name);
        }
        finally
        {
            // 确保释放标志被设置
            this._disposed = true;
        }
    }

    /// <summary>
    /// 派生类可重写此方法以实现特定的资源清理逻辑
    /// 此方法在释放过程中仅被调用一次
    /// </summary>
    /// <returns>表示异步清理操作的任务</returns>
    protected virtual ValueTask DisposeAsyncCore()
    {
        // 默认实现不执行任何操作 - 在派生类中重写以添加特定的清理逻辑
        return ValueTask.CompletedTask;
    }
}

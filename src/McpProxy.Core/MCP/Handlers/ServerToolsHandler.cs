namespace McpProxy;

/// <summary>
/// 服务器工具处理器，使用指定的MCP服务器发现策略加载工具并通过MCP服务器公开工具
/// </summary>
/// <param name="serverDiscoveryStrategy">MCP服务器发现策略实例</param>
/// <param name="logger">日志记录器实例</param>
public sealed class ServerToolsHandler(IMcpServerDiscoveryStrategy serverDiscoveryStrategy, ILogger<ServerToolsHandler> logger) : BaseMcpToolsHandler(logger)
{
    /// <summary>
    /// 获取MCP服务器发现策略实例
    /// </summary>
    private readonly IMcpServerDiscoveryStrategy _serverDiscoveryStrategy = serverDiscoveryStrategy;

    /// <summary>
    /// 获取用于初始化过程的信号量，确保线程安全的单次初始化
    /// </summary>
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

    /// <summary>
    /// 获取工具名称到客户端的映射字典，用于快速查找工具所属的服务器和客户端
    /// </summary>
    private readonly Dictionary<string, (string ServerName, McpClient Client)> _toolClientMap = [];

    /// <summary>
    /// 获取已发现的MCP客户端列表
    /// </summary>
    private readonly List<McpClient> _discoveredClients = [];

    /// <summary>
    /// 标识是否已完成初始化
    /// </summary>
    private bool _isInitialized = false;

    /// <summary>
    /// 获取或设置创建MCP客户端时使用的客户端选项
    /// </summary>
    public McpClientOptions ClientOptions { get; set; } = new McpClientOptions();

    /// <inheritdoc/>
    public override async ValueTask<ListToolsResult> ListToolsAsync(string? mcpServerName = "", CancellationToken cancellationToken = default)
    {
        // 确保已完成初始化
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // 创建空的工具列表结果
        ListToolsResult allToolResult = new()
        {
            Tools = []
        };

        // 默认使用所有已发现的客户端
        List<McpClient> clientList = this._discoveredClients;

        // 如果指定了服务器名称，则只使用匹配的客户端
        if (!string.IsNullOrWhiteSpace(mcpServerName))
        {
            clientList = [.. this._discoveredClients.Where(c => string.Equals(c.ServerInfo.Name, mcpServerName, StringComparison.OrdinalIgnoreCase))];
        }

        // 遍历所有客户端，收集工具列表
        foreach (McpClient mcpClient in clientList)
        {
            // 从客户端获取工具列表
            IList<McpClientTool> toolResult = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            // 提取协议工具对象
            IEnumerable<Tool> filteredTools = toolResult.Select(t => t.ProtocolTool);

            // 将工具添加到结果集合中
            foreach (Tool tool in filteredTools)
            {
                allToolResult.Tools.Add(tool);
            }
        }

        return allToolResult;
    }

    /// <inheritdoc/>
    public override async ValueTask<CallToolResult> CallToolsAsync(CallToolRequestParams? callToolRequestParams, CancellationToken cancellationToken = default)
    {
        // 验证参数是否为null
        if (callToolRequestParams == null)
        {
            TextContentBlock content = new()
            {
                Text = "Cannot call tools with null context parameters.",
            };

            this._logger.LogWarning(content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }

        // 确保工具客户端映射已初始化
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // 尝试查找工具对应的客户端
        if (!this._toolClientMap.TryGetValue(callToolRequestParams.Name, out var toolClient) || toolClient.Client is null)
        {
            TextContentBlock content = new()
            {
                Text = $"Tool '{callToolRequestParams.Name}' not found in the configured server.",
            };

            this._logger.LogWarning(content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true
            };
        }

        // 转换参数格式
        Dictionary<string, object?> parameters = TransformArgumentsToDictionary(callToolRequestParams.Arguments);

        // 调用工具并返回结果
        return await toolClient.Client.CallToolAsync(toolName: callToolRequestParams.Name, parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 将工具调用参数从JsonElement字典转换为对象字典
    /// 这个转换是因为McpClientExtensions.CallToolAsync方法期望参数为Dictionary&lt;string, object?&gt;类型
    /// </summary>
    /// <param name="args">JsonElement格式的参数字典</param>
    /// <returns>转换后的对象字典</returns>
    private static Dictionary<string, object?> TransformArgumentsToDictionary(IDictionary<string, JsonElement>? args)
    {
        // 如果参数为null，返回空字典
        if (args is null)
        {
            return [];
        }

        // 将JsonElement转换为object
        return args.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
    }

    /// <summary>
    /// 初始化工具客户端映射，通过发现服务器并填充工具信息
    /// 使用双重检查锁定模式确保线程安全的单次初始化
    /// </summary>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步初始化操作的任务</returns>
    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 第一次检查：快速路径，避免不必要的锁定
        if (this._isInitialized)
        {
            return;
        }

        // 获取信号量锁
        await this._initializationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // 第二次检查：在获取锁后再次确认仍未初始化
            if (this._isInitialized)
            {
                return;
            }

            // 发现所有可用的MCP服务器
            IEnumerable<IMcpServerProvider> servers = await this._serverDiscoveryStrategy.DiscoverServersAsync(cancellationToken).ConfigureAwait(false);

            // 遍历每个服务器提供者
            foreach (IMcpServerProvider server in servers)
            {
                // 创建服务器元数据
                McpServerMetadata serverMetadata = server.CreateMetadata();

                McpClient? mcpClient;

                try
                {
                    // 尝试获取或创建MCP客户端
                    mcpClient = await this._serverDiscoveryStrategy.GetOrCreateClientAsync(
                        serverMetadata.Name,
                        this.CreateClientOptions(serverMetadata.Name, serverMetadata.Title),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (InvalidOperationException ex)
                {
                    // 记录客户端创建失败，但继续处理其他服务器
                    this._logger.LogWarning("Failed to create client for provider {ProviderName}: {Error}", serverMetadata.Name, ex.Message);
                    continue;
                }
                catch (Exception ex)
                {
                    // 记录客户端启动失败，但继续处理其他服务器
                    this._logger.LogWarning("Failed to start client for provider {ProviderName}: {Error}", serverMetadata.Name, ex.Message);
                    continue;
                }

                // 验证客户端是否创建成功
                if (mcpClient is null)
                {
                    this._logger.LogWarning("Failed to get MCP client for provider {ProviderName}.", serverMetadata.Name);
                    continue;
                }

                // 缓存发现的MCP客户端
                this._discoveredClients.Add(mcpClient);

                // 获取客户端支持的所有工具
                IEnumerable<McpClientTool> clientTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                // 提取协议工具对象
                IEnumerable<Tool> tools = clientTools.Select(t => t.ProtocolTool);

                // 将每个工具映射到其所属的服务器和客户端
                foreach (Tool tool in tools)
                {
                    this._toolClientMap[tool.Name] = (serverMetadata.Name, mcpClient);
                }
            }

            // 标记初始化完成
            this._isInitialized = true;
        }
        finally
        {
            // 释放信号量锁
            this._initializationSemaphore.Release();
        }
    }

    /// <summary>
    /// 释放此工具处理器拥有的资源
    /// 清空集合并释放初始化信号量
    /// 注意：MCP客户端由发现策略拥有，不在此处释放
    /// </summary>
    /// <returns>表示异步清理操作的任务</returns>
    protected override async ValueTask DisposeAsyncCore()
    {
        // 释放初始化信号量
        this._initializationSemaphore?.Dispose();

        // 清理对客户端的引用
        this._discoveredClients.Clear();
        this._toolClientMap.Clear();

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}
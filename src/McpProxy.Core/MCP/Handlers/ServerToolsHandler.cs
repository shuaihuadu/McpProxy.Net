namespace McpProxy;

/// <summary>
/// ServerToolsHandler 从mcp.json配置的MCP服务器加载工具并通过MCP服务器公开工具
/// </summary>
public sealed class ServerToolsHandler(IMcpServerDiscoveryStrategy serverDiscoveryStrategy, ILogger<ServerToolsHandler> logger) : BaseMcpToolsHandler(logger)
{
    private readonly IMcpServerDiscoveryStrategy _serverDiscoveryStrategy = serverDiscoveryStrategy;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

    private readonly Dictionary<string, (string ServerName, McpClient Client)> _toolClientMap = [];
    private readonly List<McpClient> _discoveredClients = [];

    private bool _isInitialized = false;

    /// <summary>
    /// 获取或设置创建MCP客户端时使用的客户端选项
    /// </summary>
    public McpClientOptions ClientOptions { get; set; } = new McpClientOptions();

    /// <inheritdoc/>
    public override async ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken = default)
    {
        await this.InitializeAsync(cancellationToken);

        ListToolsResult allToolResult = new()
        {
            Tools = []
        };

        foreach (McpClient mcpClient in this._discoveredClients)
        {
            IList<McpClientTool> toolResult = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

            IEnumerable<Tool> filteredTools = toolResult
                .Select(t => t.ProtocolTool);

            foreach (Tool tool in filteredTools)
            {
                allToolResult.Tools.Add(tool);
            }
        }

        return allToolResult;
    }

    /// <inheritdoc/>
    public override async ValueTask<CallToolResult> CallToolsAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        if (request.Params == null)
        {
            TextContentBlock content = new()
            {
                Text = "Cannot call tools with null context parameters.",
            };

            _logger.LogWarning(content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }

        // 初始化工具客户端映射，如果尚未初始化
        await this.InitializeAsync(cancellationToken);

        if (!_toolClientMap.TryGetValue(request.Params.Name, out var toolClient) || toolClient.Client is null)
        {
            TextContentBlock content = new()
            {
                Text = $"Tool '{request.Params.Name}' not found in the configured server.",
            };

            _logger.LogWarning(content.Text);

            return new CallToolResult
            {
                Content = [content],
                IsError = true
            };
        }

        Dictionary<string, object?> parameters = TransformArgumentsToDictionary(request.Params.Arguments);

        return await toolClient.Client.CallToolAsync(toolName: request.Params.Name, parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 将工具调用参数转换为参数字典
    /// 这个转换是因为McpClientExtensions.CallToolAsync期望参数为Dictionary&lt;string, object?&gt;
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private static Dictionary<string, object?> TransformArgumentsToDictionary(IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null)
        {
            return [];
        }

        return args.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
    }

    /// <summary>
    /// 初始化工具客户端映射，通过发现服务器并填充工具
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (this._isInitialized)
        {
            return;
        }

        await this._initializationSemaphore.WaitAsync(cancellationToken);

        try
        {
            // 双重检查：在获取锁后再次确认仍未初始化
            if (_isInitialized)
            {
                return;
            }

            IEnumerable<IMcpServerProvider> servers = await this._serverDiscoveryStrategy.DiscoverServersAsync(cancellationToken);

            foreach (IMcpServerProvider server in servers)
            {
                McpServerMetadata serverMetadata = server.CreateMetadata();

                McpClient? mcpClient;

                try
                {
                    mcpClient = await this._serverDiscoveryStrategy.GetOrCreateClientAsync(serverMetadata.Name, CreateClientOptions(serverMetadata.Name, serverMetadata.Title), cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning("Failed to create client for provider {ProviderName}: {Error}", serverMetadata.Name, ex.Message);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to start client for provider {ProviderName}: {Error}", serverMetadata.Name, ex.Message);
                    continue;
                }

                if (mcpClient is null)
                {
                    _logger.LogWarning("Failed to get MCP client for provider {ProviderName}.", serverMetadata.Name);
                    continue;
                }

                // 缓存发现的MCP客户端
                this._discoveredClients.Add(mcpClient);

                IEnumerable<McpClientTool> clientTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);

                IEnumerable<Tool> tools = clientTools.Select(t => t.ProtocolTool);

                foreach (Tool tool in tools)
                {
                    this._toolClientMap[tool.Name] = (serverMetadata.Name, mcpClient);
                }
            }

            this._isInitialized = true;
        }
        finally
        {
            this._initializationSemaphore.Release();
        }
    }

    /// <summary>
    /// 释放此工具加载器拥有的资源
    /// 清空集合并释放初始化信号量
    /// 注意：MCP客户端由发现策略拥有，不在此处释放
    /// </summary>
    protected override async ValueTask DisposeAsyncCore()
    {
        // 只释放拥有的资源，而不是MCP客户端
        this._initializationSemaphore?.Dispose();

        // 清理对客户端的引用
        this._discoveredClients.Clear();
        this._toolClientMap.Clear();

        await ValueTask.CompletedTask;
    }
}
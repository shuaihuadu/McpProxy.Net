// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy;

/// <summary>
/// Stdio 到 HTTP 代理服务的完整实现
/// 使用指定的MCP服务器发现策略加载工具、提示词和资源，并通过HTTP服务公开
/// </summary>
/// <param name="serverDiscoveryStrategy">MCP服务器发现策略实例</param>
/// <param name="logger">日志记录器实例</param>
public sealed class StdioToHttpProxyService(IMcpServerDiscoveryStrategy serverDiscoveryStrategy, ILogger<StdioToHttpProxyService> logger) : BaseStdioToHttpProxyService(logger)
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
    /// 获取提示词名称到客户端的映射字典，用于快速查找提示词所属的服务器和客户端
    /// </summary>
    private readonly Dictionary<string, (string ServerName, McpClient Client)> _promptClientMap = [];

    /// <summary>
    /// 获取资源URI到客户端的映射字典，用于快速查找资源所属的服务器和客户端
    /// </summary>
    private readonly Dictionary<string, (string ServerName, McpClient Client)> _resourceClientMap = [];

    /// <summary>
    /// 获取已发现的MCP客户端列表
    /// </summary>
    private readonly List<McpClient> _discoveredClients = [];

    /// <summary>
    /// 标识是否已完成初始化
    /// </summary>
    private bool _isInitialized = false;

    /// <summary>
    /// 获取最后一次初始化的时间
    /// </summary>
    private DateTime? _lastInitializedAt = null;

    /// <summary>
    /// 刷新计数器，用于日志和调试
    /// </summary>
    private int _refreshCount = 0;

    /// <summary>
    /// 获取或设置创建MCP客户端时使用的客户端选项
    /// </summary>
    public McpClientOptions ClientOptions { get; set; } = new McpClientOptions();

    // ========== Tools 实现 ==========

    /// <inheritdoc/>
    public override async ValueTask<ListToolsResult> ListToolsAsync(string? mcpServerName = null, string? cursor = null, CancellationToken cancellationToken = default)
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
            try
            {
                // 从客户端获取工具列表（注意：当前SDK版本的ListToolsAsync不支持cursor参数）
                IList<McpClientTool> toolResult = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                // 提取协议工具对象
                IEnumerable<Tool> filteredTools = toolResult.Select(t => t.ProtocolTool);

                // 将工具添加到结果集合中
                foreach (Tool tool in filteredTools)
                {
                    allToolResult.Tools.Add(tool);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Failed to list tools from server {ServerName}", mcpClient.ServerInfo.Name);
            }
        }

        // TODO: 实现分页支持 - 当前忽略cursor参数
        if (!string.IsNullOrEmpty(cursor))
        {
            this._logger.LogWarning("Pagination with cursor is not yet supported for tools listing.");
        }

        return allToolResult;
    }

    /// <inheritdoc/>
    public override async ValueTask<CallToolResult> CallToolAsync(CallToolRequestParams callToolRequestParams, CancellationToken cancellationToken = default)
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

    // ========== Prompts 实现 ==========

    /// <inheritdoc/>
    public override async ValueTask<ListPromptsResult> ListPromptsAsync(string? mcpServerName = null, string? cursor = null, CancellationToken cancellationToken = default)
    {
        // 确保已完成初始化
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // 创建空的提示词列表结果
        ListPromptsResult allPromptsResult = new()
        {
            Prompts = []
        };

        // 默认使用所有已发现的客户端
        List<McpClient> clientList = this._discoveredClients;

        // 如果指定了服务器名称，则只使用匹配的客户端
        if (!string.IsNullOrWhiteSpace(mcpServerName))
        {
            clientList = [.. this._discoveredClients.Where(c => string.Equals(c.ServerInfo.Name, mcpServerName, StringComparison.OrdinalIgnoreCase))];
        }

        // 遍历所有客户端，收集提示词列表
        foreach (McpClient mcpClient in clientList)
        {
            try
            {
                // 从客户端获取提示词列表
                IList<McpClientPrompt> promptResult = await mcpClient.ListPromptsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                // 提取协议提示词对象
                IEnumerable<Prompt> filteredPrompts = promptResult.Select(p => p.ProtocolPrompt);

                // 将提示词添加到结果集合中
                foreach (Prompt prompt in filteredPrompts)
                {
                    allPromptsResult.Prompts.Add(prompt);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Failed to list prompts from server {ServerName}", mcpClient.ServerInfo.Name);
            }
        }

        // TODO: 实现分页支持 - 当前忽略cursor参数
        if (!string.IsNullOrEmpty(cursor))
        {
            this._logger.LogWarning("Pagination with cursor is not yet supported for prompts listing.");
        }

        return allPromptsResult;
    }

    /// <inheritdoc/>
    public override async ValueTask<GetPromptResult> GetPromptAsync(GetPromptRequestParams getPromptRequestParams, CancellationToken cancellationToken = default)
    {
        // 验证参数是否为null
        if (getPromptRequestParams == null)
        {
            this._logger.LogWarning("Cannot get prompt with null parameters.");
            throw new ArgumentNullException(nameof(getPromptRequestParams));
        }

        // 确保提示词客户端映射已初始化
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // 尝试查找提示词对应的客户端
        if (!this._promptClientMap.TryGetValue(getPromptRequestParams.Name, out var promptClient) || promptClient.Client is null)
        {
            this._logger.LogWarning("Prompt '{PromptName}' not found in the configured server.", getPromptRequestParams.Name);
            throw new InvalidOperationException($"Prompt '{getPromptRequestParams.Name}' not found in the configured server.");
        }

        // 转换参数格式
        Dictionary<string, object?> parameters = TransformArgumentsToDictionary(getPromptRequestParams.Arguments);

        // 获取提示词并返回结果
        return await promptClient.Client.GetPromptAsync(getPromptRequestParams.Name, parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    // ========== Resources 实现 ==========

    /// <inheritdoc/>
    public override async ValueTask<ListResourcesResult> ListResourcesAsync(string? mcpServerName = null, string? cursor = null, CancellationToken cancellationToken = default)
    {
        // 确保已完成初始化
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // 创建空的资源列表结果
        ListResourcesResult allResourcesResult = new()
        {
            Resources = []
        };

        // 默认使用所有已发现的客户端
        List<McpClient> clientList = this._discoveredClients;

        // 如果指定了服务器名称，则只使用匹配的客户端
        if (!string.IsNullOrWhiteSpace(mcpServerName))
        {
            clientList = [.. this._discoveredClients.Where(c => string.Equals(c.ServerInfo.Name, mcpServerName, StringComparison.OrdinalIgnoreCase))];
        }

        // 遍历所有客户端，收集资源列表
        foreach (McpClient mcpClient in clientList)
        {
            try
            {
                // 从客户端获取资源列表
                IList<McpClientResource> resourceResult = await mcpClient.ListResourcesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                // 提取协议资源对象
                IEnumerable<Resource> filteredResources = resourceResult.Select(r => r.ProtocolResource);

                // 将资源添加到结果集合中
                foreach (Resource resource in filteredResources)
                {
                    allResourcesResult.Resources.Add(resource);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Failed to list resources from server {ServerName}", mcpClient.ServerInfo.Name);
            }
        }

        // TODO: 实现分页支持 - 当前忽略cursor参数
        if (!string.IsNullOrEmpty(cursor))
        {
            this._logger.LogWarning("Pagination with cursor is not yet supported for resources listing.");
        }

        return allResourcesResult;
    }

    /// <inheritdoc/>
    public override async ValueTask<ReadResourceResult> ReadResourceAsync(ReadResourceRequestParams readResourceRequestParams, CancellationToken cancellationToken = default)
    {
        // 验证参数是否为null
        if (readResourceRequestParams == null)
        {
            this._logger.LogWarning("Cannot read resource with null parameters.");
            throw new ArgumentNullException(nameof(readResourceRequestParams));
        }

        // 确保资源客户端映射已初始化
        await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // 尝试查找资源对应的客户端
        if (!this._resourceClientMap.TryGetValue(readResourceRequestParams.Uri, out var resourceClient) || resourceClient.Client is null)
        {
            this._logger.LogWarning("Resource '{ResourceUri}' not found in the configured server.", readResourceRequestParams.Uri);
            throw new InvalidOperationException($"Resource '{readResourceRequestParams.Uri}' not found in the configured server.");
        }

        // 读取资源并返回结果
        return await resourceClient.Client.ReadResourceAsync(readResourceRequestParams.Uri, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override ValueTask SubscribeResourceAsync(SubscribeRequestParams subscribeRequestParams, CancellationToken cancellationToken = default)
    {
        // TODO: 当MCP SDK支持资源订阅功能后实现
        // 当前SDK版本暂不支持Subscribe操作
        this._logger.LogWarning("Resource subscription is not yet supported by the current MCP SDK version.");
        throw new NotImplementedException("Resource subscription functionality is not yet supported by the current MCP SDK version.");
    }

    /// <inheritdoc/>
    public override ValueTask UnsubscribeResourceAsync(UnsubscribeRequestParams unsubscribeRequestParams, CancellationToken cancellationToken = default)
    {
        // TODO: 当MCP SDK支持资源取消订阅功能后实现
        // 当前SDK版本暂不支持Unsubscribe操作
        this._logger.LogWarning("Resource unsubscription is not yet supported by the current MCP SDK version.");
        throw new NotImplementedException("Resource unsubscription functionality is not yet supported by the current MCP SDK version.");
    }

    // ========== 辅助方法 ==========

    /// <summary>
    /// 将参数从JsonElement字典转换为对象字典
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
    /// 初始化所有客户端映射，通过发现服务器并填充工具、提示词和资源信息
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

                // 初始化工具映射
                await this.InitializeToolsAsync(mcpClient, serverMetadata.Name, cancellationToken).ConfigureAwait(false);

                // 初始化提示词映射
                await this.InitializePromptsAsync(mcpClient, serverMetadata.Name, cancellationToken).ConfigureAwait(false);

                // 初始化资源映射
                await this.InitializeResourcesAsync(mcpClient, serverMetadata.Name, cancellationToken).ConfigureAwait(false);
            }

            // 标记初始化完成
            this._isInitialized = true;
            this._lastInitializedAt = DateTime.UtcNow;
            
            this._logger.LogInformation(
                "Initialization completed. Discovered {ServerCount} servers, {ToolCount} tools, {PromptCount} prompts, {ResourceCount} resources.",
                this._discoveredClients.Count,
                this._toolClientMap.Count,
                this._promptClientMap.Count,
                this._resourceClientMap.Count);
        }
        finally
        {
            // 释放信号量锁
            this._initializationSemaphore.Release();
        }
    }

    /// <summary>
    /// 初始化工具映射
    /// </summary>
    private async Task InitializeToolsAsync(McpClient mcpClient, string serverName, CancellationToken cancellationToken)
    {
        try
        {
            // 获取客户端支持的所有工具
            IEnumerable<McpClientTool> clientTools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            // 提取协议工具对象
            IEnumerable<Tool> tools = clientTools.Select(t => t.ProtocolTool);

            // 将每个工具映射到其所属的服务器和客户端
            foreach (Tool tool in tools)
            {
                this._toolClientMap[tool.Name] = (serverName, mcpClient);
            }

            this._logger.LogInformation("Initialized {Count} tools from server {ServerName}", tools.Count(), serverName);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to initialize tools from server {ServerName}", serverName);
        }
    }

    /// <summary>
    /// 初始化提示词映射
    /// </summary>
    private async Task InitializePromptsAsync(McpClient mcpClient, string serverName, CancellationToken cancellationToken)
    {
        try
        {
            // 获取客户端支持的所有提示词
            IEnumerable<McpClientPrompt> clientPrompts = await mcpClient.ListPromptsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            // 提取协议提示词对象
            IEnumerable<Prompt> prompts = clientPrompts.Select(p => p.ProtocolPrompt);

            // 将每个提示词映射到其所属的服务器和客户端
            foreach (Prompt prompt in prompts)
            {
                this._promptClientMap[prompt.Name] = (serverName, mcpClient);
            }

            this._logger.LogInformation("Initialized {Count} prompts from server {ServerName}", prompts.Count(), serverName);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to initialize prompts from server {ServerName}", serverName);
        }
    }

    /// <summary>
    /// 初始化资源映射
    /// </summary>
    private async Task InitializeResourcesAsync(McpClient mcpClient, string serverName, CancellationToken cancellationToken)
    {
        try
        {
            // 获取客户端支持的所有资源
            IEnumerable<McpClientResource> clientResources = await mcpClient.ListResourcesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            // 提取协议资源对象
            IEnumerable<Resource> resources = clientResources.Select(r => r.ProtocolResource);

            // 将每个资源映射到其所属的服务器和客户端
            foreach (Resource resource in resources)
            {
                this._resourceClientMap[resource.Uri] = (serverName, mcpClient);
            }

            this._logger.LogInformation("Initialized {Count} resources from server {ServerName}", resources.Count(), serverName);
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to initialize resources from server {ServerName}", serverName);
        }
    }

    // ========== 生命周期管理实现 ==========

    /// <inheritdoc/>
    public override Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return this.RefreshInternalAsync(cancellationToken);
    }

    /// <summary>
    /// 内部刷新实现
    /// 清空现有缓存并重新初始化所有映射
    /// </summary>
    private async Task RefreshInternalAsync(CancellationToken cancellationToken)
    {
        int refreshNumber = Interlocked.Increment(ref this._refreshCount);
        
        this._logger.LogInformation("Starting cache refresh operation #{RefreshCount}...", refreshNumber);

        await this._initializationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // 1. 标记为未初始化
            this._isInitialized = false;

            this._logger.LogDebug("Clearing internal caches...");

            // 2. 清空所有缓存映射
            this._toolClientMap.Clear();
            this._promptClientMap.Clear();
            this._resourceClientMap.Clear();

            // 3. 清空客户端列表
            this._discoveredClients.Clear();

            // 4. 重新初始化（重用现有的初始化逻辑）
            await this.InitializeAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Cache refresh #{RefreshCount} completed successfully. " +
                "Discovered {ServerCount} servers, {ToolCount} tools, {PromptCount} prompts, {ResourceCount} resources.",
                refreshNumber,
                this._discoveredClients.Count,
                this._toolClientMap.Count,
                this._promptClientMap.Count,
                this._resourceClientMap.Count);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error occurred during cache refresh #{RefreshCount}", refreshNumber);
            throw;
        }
        finally
        {
            this._initializationSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public override ServiceStatusInfo GetStatus()
    {
        return new ServiceStatusInfo
        {
            IsInitialized = this._isInitialized,
            LastInitializedAt = this._lastInitializedAt,
            TotalServers = this._discoveredClients.Count,
            TotalTools = this._toolClientMap.Count,
            TotalPrompts = this._promptClientMap.Count,
            TotalResources = this._resourceClientMap.Count,
            ServerNames = this._discoveredClients.Select(c => c.ServerInfo.Name).ToList()
        };
    }

    /// <inheritdoc/>
    public override async Task<HealthCheckResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Starting health check for {ServerCount} servers...", this._discoveredClients.Count);

        var serverHealths = new Dictionary<string, ServerHealth>();
        int healthyCount = 0;
        int unhealthyCount = 0;
        DateTime checkTime = DateTime.UtcNow;

        foreach (McpClient client in this._discoveredClients)
        {
            string serverName = client.ServerInfo.Name;
            
            try
            {
                // 尝试调用一个简单的操作来验证连接
                // 使用 ListToolsAsync 作为健康检查，因为它是最基础的操作
                _ = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                serverHealths[serverName] = new ServerHealth
                {
                    ServerName = serverName,
                    IsConnected = true,
                    ErrorMessage = null,
                    LastCheckTime = checkTime
                };

                healthyCount++;

                this._logger.LogDebug("Server {ServerName} is healthy", serverName);
            }
            catch (Exception ex)
            {
                serverHealths[serverName] = new ServerHealth
                {
                    ServerName = serverName,
                    IsConnected = false,
                    ErrorMessage = ex.Message,
                    LastCheckTime = checkTime
                };

                unhealthyCount++;

                this._logger.LogWarning(ex, "Server {ServerName} health check failed", serverName);
            }
        }

        bool isHealthy = unhealthyCount == 0 && healthyCount > 0;

        this._logger.LogInformation(
            "Health check completed. Healthy: {HealthyCount}, Unhealthy: {UnhealthyCount}, Overall: {OverallStatus}",
            healthyCount,
            unhealthyCount,
            isHealthy ? "Healthy" : "Unhealthy");

        return new HealthCheckResult
        {
            IsHealthy = isHealthy,
            HealthyServers = healthyCount,
            UnhealthyServers = unhealthyCount,
            ServerHealths = serverHealths
        };
    }

    /// <summary>
    /// 释放此代理服务拥有的资源
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
        this._promptClientMap.Clear();
        this._resourceClientMap.Clear();

        await ValueTask.CompletedTask.ConfigureAwait(false);
    }
}

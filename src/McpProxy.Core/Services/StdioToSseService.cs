using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using McpProxy.Core.Configuration;
using McpProxy.Abstractions.Services;
using McpProxy.Abstractions.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace McpProxy.Core.Services;

/// <summary>
/// 实现Stdio到SSE/HTTP的核心业务逻辑
/// 该服务连接到一个或多个本地Stdio MCP服务器并提供聚合查询功能
/// </summary>
public sealed class StdioToSseService : IStdioToSseService, IAsyncDisposable
{
    private readonly StdioServersOptions _options;
    private readonly ILogger<StdioToSseService> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ConcurrentDictionary<string, ServerConnection> _servers;
    private bool _initialized;

    /// <summary>
    /// 初始化 <see cref="StdioToSseService"/> 类的新实例
    /// </summary>
    /// <param name="options">Stdio服务器配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="loggerFactory">日志工厂（可选）</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="options"/> 或 <paramref name="logger"/> 为 null 时抛出</exception>
    public StdioToSseService(
        IOptions<StdioServersOptions> options,
        ILogger<StdioToSseService> logger,
        ILoggerFactory? loggerFactory = null)
    {
        this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._loggerFactory = loggerFactory;
        this._servers = new ConcurrentDictionary<string, ServerConnection>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 如果已经初始化，直接返回
        if (this._initialized)
        {
            return;
        }

        this._logger.LogInformation(
            "Initializing Stdio to SSE service with {ServerCount} servers",
            this._options.Servers.Count);

        // 创建所有启用服务器的连接任务
        List<Task> connectionTasks = new List<Task>();
        foreach (McpServerConfig serverConfig in this._options.Servers.Where(s => s.Enabled))
        {
            connectionTasks.Add(this.ConnectToServerAsync(serverConfig, cancellationToken));
        }

        // 并发等待所有服务器连接完成
        await Task.WhenAll(connectionTasks).ConfigureAwait(false);

        this._logger.LogInformation(
            "Connected to {ConnectedCount}/{TotalCount} servers",
            this._servers.Count,
            this._options.Servers.Count(s => s.Enabled));

        // 如果没有任何服务器连接成功，抛出异常
        if (this._servers.IsEmpty)
        {
            throw new InvalidOperationException("Failed to connect to any MCP servers");
        }

        this._initialized = true;
    }

    /// <inheritdoc />
    public async Task<ListToolsResult> ListToolsAsync(
        string? serverFilter = null,
        CancellationToken cancellationToken = default)
    {
        this.EnsureInitialized();

        // 如果指定了服务器过滤器且允许过滤，则只查询该服务器
        if (!string.IsNullOrEmpty(serverFilter) && this._options.AllowServerFilter)
        {
            this._logger.LogDebug("Listing tools for server: {ServerName}", serverFilter);
            return await this.ListToolsFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
        }

        // 否则查询所有服务器并聚合结果
        this._logger.LogDebug("Listing tools from all servers");
        return await this.ListAllToolsAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CallToolResult> CallToolAsync(
        string toolName,
        object? arguments = null,
        CancellationToken cancellationToken = default)
    {
        this.EnsureInitialized();

        // 解析工具名称，提取服务器名称和实际工具名称
        (string serverName, string actualToolName) = this.ParseToolName(toolName);

        this._logger.LogDebug(
            "Calling tool '{ToolName}' on server '{ServerName}'",
            actualToolName,
            serverName);

        // 查找目标服务器连接
        if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
        {
            throw new InvalidOperationException($"Server '{serverName}' not found or not connected");
        }

        // 调用工具并返回结果
        CallToolResult result = await connection.Client.CallToolAsync(
            new CallToolRequestParams
            {
                Name = actualToolName,
                Arguments = arguments as IDictionary<string, JsonElement>
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<ListPromptsResult> ListPromptsAsync(
        string? serverFilter = null,
        CancellationToken cancellationToken = default)
    {
        this.EnsureInitialized();

        // 如果指定了服务器过滤器且允许过滤，则只查询该服务器
        if (!string.IsNullOrEmpty(serverFilter) && this._options.AllowServerFilter)
        {
            return await this.ListPromptsFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
        }

        // 否则查询所有服务器并聚合结果
        return await this.ListAllPromptsAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GetPromptResult> GetPromptAsync(
        string promptName,
        object? arguments = null,
        CancellationToken cancellationToken = default)
    {
        this.EnsureInitialized();

        // 解析提示名称，提取服务器名称和实际提示名称
        (string serverName, string actualPromptName) = this.ParseToolName(promptName);

        // 查找目标服务器连接
        if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
        {
            throw new InvalidOperationException($"Server '{serverName}' not found or not connected");
        }

        // 获取提示并返回结果
        GetPromptResult result = await connection.Client.GetPromptAsync(
            new GetPromptRequestParams
            {
                Name = actualPromptName,
                Arguments = arguments as IDictionary<string, JsonElement>
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<ListResourcesResult> ListResourcesAsync(
        string? serverFilter = null,
        CancellationToken cancellationToken = default)
    {
        this.EnsureInitialized();

        // 如果指定了服务器过滤器且允许过滤，则只查询该服务器
        if (!string.IsNullOrEmpty(serverFilter) && this._options.AllowServerFilter)
        {
            return await this.ListResourcesFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
        }

        // 否则查询所有服务器并聚合结果
        return await this.ListAllResourcesAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReadResourceResult> ReadResourceAsync(
        string resourceUri,
        CancellationToken cancellationToken = default)
    {
        this.EnsureInitialized();

        // 解析资源URI，提取服务器名称和实际URI
        (string serverName, string actualUri) = this.ParseResourceUri(resourceUri);

        // 查找目标服务器连接
        if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
        {
            throw new InvalidOperationException($"Server '{serverName}' not found or not connected");
        }

        // 读取资源并返回结果
        ReadResourceResult result = await connection.Client.ReadResourceAsync(
            new ReadResourceRequestParams { Uri = actualUri },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ServerStatusInfo>> GetServerStatusAsync()
    {
        this.EnsureInitialized();

        // 收集所有服务器的状态信息
        IReadOnlyList<ServerStatusInfo> status = this._servers.Values.Select(s => new ServerStatusInfo
        {
            Name = s.Name,
            IsConnected = s.IsConnected,
            ServerName = s.ServerInfo?.Name,
            ServerVersion = s.ServerInfo?.Version,
            LastHeartbeat = s.LastHeartbeat,
            Capabilities = s.Capabilities
        }).ToList();

        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public ServerCapabilities GetAggregatedCapabilities()
    {
        this.EnsureInitialized();

        ServerCapabilities capabilities = new ServerCapabilities();

        // 如果任何服务器支持工具，则聚合服务器也支持
        if (this._servers.Values.Any(s => s.Capabilities?.Tools != null))
        {
            capabilities.Tools = new ToolsCapability();
        }

        // 如果任何服务器支持提示，则聚合服务器也支持
        if (this._servers.Values.Any(s => s.Capabilities?.Prompts != null))
        {
            capabilities.Prompts = new PromptsCapability();
        }

        // 如果任何服务器支持资源，则聚合服务器也支持
        if (this._servers.Values.Any(s => s.Capabilities?.Resources != null))
        {
            capabilities.Resources = new ResourcesCapability();
        }

        return capabilities;
    }

    /// <summary>
    /// 连接到指定的Stdio服务器
    /// </summary>
    /// <param name="config">服务器配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    private async Task ConnectToServerAsync(McpServerConfig config, CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation(
                "Connecting to server '{ServerName}': {Command} {Arguments}",
                config.Name,
                config.Command,
                string.Join(" ", config.Arguments ?? new List<string>()));

            // 创建Stdio客户端传输选项
            StdioClientTransportOptions transportOptions = new StdioClientTransportOptions
            {
                Command = config.Command,
                Arguments = config.Arguments,
                EnvironmentVariables = config.Environment as IDictionary<string, string?>,
                WorkingDirectory = config.WorkingDirectory
            };

            // 创建Stdio传输层
            StdioClientTransport transport = new StdioClientTransport(
                transportOptions,
                loggerFactory: this._loggerFactory);

            // 创建并初始化MCP客户端
            McpClient client = await McpClient.CreateAsync(
                transport,
                clientOptions: new McpClientOptions
                {
                    ClientInfo = new Implementation
                    {
                        Name = "mcp-proxy",
                        Version = "1.0.0"
                    }
                },
                loggerFactory: this._loggerFactory,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // 创建服务器连接对象
            ServerConnection connection = new ServerConnection
            {
                Name = config.Name,
                Client = client,
                Transport = transport,
                ServerInfo = client.ServerInfo,
                Capabilities = client.ServerCapabilities,
                IsConnected = true,
                LastHeartbeat = DateTime.UtcNow
            };

            // 将连接添加到字典中
            this._servers[config.Name] = connection;

            this._logger.LogInformation(
                "Connected to server '{ServerName}': {ServerTitle} ({ServerVersion})",
                config.Name,
                client.ServerInfo?.Name ?? "Unknown",
                client.ServerInfo?.Version ?? "Unknown");
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to connect to server '{ServerName}': {Error}",
                config.Name,
                ex.Message);
        }
    }

    /// <summary>
    /// 解析带命名空间前缀的工具或提示名称
    /// </summary>
    /// <param name="fullName">完整名称，格式为 "servername:itemname" 或 "itemname"</param>
    /// <returns>包含服务器名称和项目名称的元组</returns>
    /// <exception cref="ArgumentException">当名称为空时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置了多个服务器但名称不包含前缀时抛出</exception>
    private (string serverName, string itemName) ParseToolName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            throw new ArgumentException("Item name cannot be empty", nameof(fullName));
        }

        // 查找冒号分隔符
        int colonIndex = fullName.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0)
        {
            // 格式: "servername:itemname"
            string serverName = fullName.Substring(0, colonIndex);
            string itemName = fullName.Substring(colonIndex + 1);
            return (serverName, itemName);
        }

        // 如果只配置了一个服务器，直接使用该服务器
        if (this._servers.Count == 1)
        {
            string serverName = this._servers.Keys.First();
            return (serverName, fullName);
        }

        // 多服务器配置下必须包含前缀
        throw new InvalidOperationException(
            $"Item name '{fullName}' must include server prefix (format: 'servername:itemname') when multiple servers are configured");
    }

    /// <summary>
    /// 解析带命名空间前缀的资源URI
    /// </summary>
    /// <param name="uri">完整URI，格式为 "servername:scheme://path" 或 "scheme://path"</param>
    /// <returns>包含服务器名称和实际URI的元组</returns>
    /// <exception cref="ArgumentException">当URI为空时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置了多个服务器但URI不包含前缀时抛出</exception>
    private (string serverName, string resourceUri) ParseResourceUri(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException("Resource URI cannot be empty", nameof(uri));
        }

        // 查找 :// 的位置
        int schemeEnd = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            ReadOnlySpan<char> scheme = uri.AsSpan(0, schemeEnd);
            int colonIndex = scheme.IndexOf(':');

            if (colonIndex > 0)
            {
                // 格式: "servername:scheme://path"
                string serverName = scheme.Slice(0, colonIndex).ToString();
                string actualScheme = scheme.Slice(colonIndex + 1).ToString();
                string resourceUri = actualScheme + uri.Substring(schemeEnd);
                return (serverName, resourceUri);
            }
        }

        // 如果只配置了一个服务器，直接使用该服务器
        if (this._servers.Count == 1)
        {
            string serverName = this._servers.Keys.First();
            return (serverName, uri);
        }

        // 多服务器配置下必须包含前缀
        throw new InvalidOperationException(
            $"Resource URI '{uri}' must include server prefix when multiple servers are configured");
    }

    /// <summary>
    /// 列出指定服务器的工具
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <param name="includePrefix">是否在工具名称前添加服务器前缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含工具列表的结果</returns>
    private async Task<ListToolsResult> ListToolsFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        // 安全地查询服务器
        ListToolsResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListToolsAsync(param, cancellationToken: ct),
            new ListToolsRequestParams(),
            () => new ListToolsResult { Tools = new List<Tool>() },
            cancellationToken).ConfigureAwait(false);

        // 如果需要添加前缀，则为每个工具名称添加服务器前缀
        if (includePrefix && result.Tools != null)
        {
            foreach (Tool tool in result.Tools)
            {
                tool.Name = $"{serverName}:{tool.Name}";
            }
        }

        return result;
    }

    /// <summary>
    /// 列出所有服务器的工具并聚合
    /// </summary>
    /// <param name="includePrefix">是否在工具名称前添加服务器前缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含聚合后工具列表的结果</returns>
    private async Task<ListToolsResult> ListAllToolsAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        // 并发查询所有服务器
        List<Task<ListToolsResult>> tasks = this._servers.Values
            .Select(conn => this.ListToolsFromServerAsync(conn.Name, includePrefix, cancellationToken))
            .ToList();

        ListToolsResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 聚合所有工具
        List<Tool> allTools = results
            .SelectMany(r => r.Tools ?? Enumerable.Empty<Tool>())
            .ToList();

        return new ListToolsResult { Tools = allTools };
    }

    /// <summary>
    /// 列出指定服务器的提示
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <param name="includePrefix">是否在提示名称前添加服务器前缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含提示列表的结果</returns>
    private async Task<ListPromptsResult> ListPromptsFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        // 安全地查询服务器
        ListPromptsResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListPromptsAsync(param, cancellationToken: ct),
            new ListPromptsRequestParams(),
            () => new ListPromptsResult { Prompts = new List<Prompt>() },
            cancellationToken).ConfigureAwait(false);

        // 如果需要添加前缀，则为每个提示名称添加服务器前缀
        if (includePrefix && result.Prompts != null)
        {
            foreach (Prompt prompt in result.Prompts)
            {
                prompt.Name = $"{serverName}:{prompt.Name}";
            }
        }

        return result;
    }

    /// <summary>
    /// 列出所有服务器的提示并聚合
    /// </summary>
    /// <param name="includePrefix">是否在提示名称前添加服务器前缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含聚合后提示列表的结果</returns>
    private async Task<ListPromptsResult> ListAllPromptsAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        // 并发查询所有服务器
        List<Task<ListPromptsResult>> tasks = this._servers.Values
            .Select(conn => this.ListPromptsFromServerAsync(conn.Name, includePrefix, cancellationToken))
            .ToList();

        ListPromptsResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 聚合所有提示
        List<Prompt> allPrompts = results
            .SelectMany(r => r.Prompts ?? Enumerable.Empty<Prompt>())
            .ToList();

        return new ListPromptsResult { Prompts = allPrompts };
    }

    /// <summary>
    /// 列出指定服务器的资源
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <param name="includePrefix">是否在资源URI前添加服务器前缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含资源列表的结果</returns>
    private async Task<ListResourcesResult> ListResourcesFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        // 安全地查询服务器
        ListResourcesResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListResourcesAsync(param, cancellationToken: ct),
            new ListResourcesRequestParams(),
            () => new ListResourcesResult { Resources = new List<Resource>() },
            cancellationToken).ConfigureAwait(false);

        // 如果需要添加前缀，则为每个资源URI添加服务器前缀
        if (includePrefix && result.Resources != null)
        {
            foreach (Resource resource in result.Resources)
            {
                resource.Uri = AddServerPrefixToUri(serverName, resource.Uri);
            }
        }

        return result;
    }

    /// <summary>
    /// 列出所有服务器的资源并聚合
    /// </summary>
    /// <param name="includePrefix">是否在资源URI前添加服务器前缀</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含聚合后资源列表的结果</returns>
    private async Task<ListResourcesResult> ListAllResourcesAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        // 并发查询所有服务器
        List<Task<ListResourcesResult>> tasks = this._servers.Values
            .Select(conn => this.ListResourcesFromServerAsync(conn.Name, includePrefix, cancellationToken))
            .ToList();

        ListResourcesResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // 聚合所有资源
        List<Resource> allResources = results
            .SelectMany(r => r.Resources ?? Enumerable.Empty<Resource>())
            .ToList();

        return new ListResourcesResult { Resources = allResources };
    }

    /// <summary>
    /// 在资源URI前添加服务器前缀
    /// </summary>
    /// <param name="serverName">服务器名称</param>
    /// <param name="uri">原始URI</param>
    /// <returns>带服务器前缀的URI</returns>
    private static string AddServerPrefixToUri(string serverName, string uri)
    {
        int schemeEnd = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            // 格式: "servername:scheme://path"
            string scheme = uri.Substring(0, schemeEnd);
            string path = uri.Substring(schemeEnd);
            return $"{serverName}:{scheme}{path}";
        }

        // 无scheme的URI直接添加前缀
        return $"{serverName}:{uri}";
    }

    /// <summary>
    /// 安全地查询服务器，捕获并处理异常
    /// </summary>
    /// <typeparam name="TParams">请求参数类型</typeparam>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="serverName">服务器名称</param>
    /// <param name="queryFunc">查询函数</param>
    /// <param name="requestParams">请求参数</param>
    /// <param name="defaultResultFactory">默认结果工厂函数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果或默认结果</returns>
    private async Task<TResult> QueryServerSafelyAsync<TParams, TResult>(
        string serverName,
        Func<McpClient, TParams, CancellationToken, ValueTask<TResult>> queryFunc,
        TParams requestParams,
        Func<TResult> defaultResultFactory,
        CancellationToken cancellationToken)
        where TResult : class
    {
        // 查找服务器连接
        if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
        {
            return defaultResultFactory();
        }

        try
        {
            // 执行查询
            TResult result = await queryFunc(connection.Client, requestParams, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            // 记录错误并返回默认结果
            this._logger.LogError(ex, "Error querying server '{ServerName}'", serverName);
            return defaultResultFactory();
        }
    }

    /// <summary>
    /// 确保服务已初始化
    /// </summary>
    /// <exception cref="InvalidOperationException">当服务未初始化时抛出</exception>
    private void EnsureInitialized()
    {
        if (!this._initialized)
        {
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // 释放所有服务器连接
        foreach (ServerConnection connection in this._servers.Values)
        {
            try
            {
                // 释放MCP客户端（传输层由客户端管理）
                await connection.Client.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error disposing server '{ServerName}'", connection.Name);
            }
        }

        // 清空服务器字典
        this._servers.Clear();
    }

    /// <summary>
    /// 服务器连接信息
    /// 封装单个MCP服务器的连接状态和元数据
    /// </summary>
    private sealed class ServerConnection
    {
        /// <summary>
        /// 获取或设置服务器名称
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// 获取或设置MCP客户端实例
        /// </summary>
        public required McpClient Client { get; init; }

        /// <summary>
        /// 获取或设置Stdio传输层实例
        /// </summary>
        public required StdioClientTransport Transport { get; init; }

        /// <summary>
        /// 获取或设置服务器信息
        /// </summary>
        public Implementation? ServerInfo { get; init; }

        /// <summary>
        /// 获取或设置服务器能力
        /// </summary>
        public ServerCapabilities? Capabilities { get; init; }

        /// <summary>
        /// 获取或设置连接状态
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 获取或设置最后一次心跳时间
        /// </summary>
        public DateTime LastHeartbeat { get; set; }
    }
}

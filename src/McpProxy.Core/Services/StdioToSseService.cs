using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using McpProxy.Core.Configuration;
using McpProxy.Abstractions.Services;
using McpProxy.Abstractions.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpProxy.Core.Services;

/// <summary>
/// 实现Stdio到SSE/HTTP的核心业务逻辑
/// 该服务连接到一个或多个本地Stdio MCP服务器并提供聚合查询功能
/// 同时支持 REST API 方式和 MCP 原生协议方式
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
        List<Task> connectionTasks = [];
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

    #region IStdioToSseService 接口实现（REST API 方式）

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

        ServerCapabilities capabilities = new();

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

    #endregion

    #region MCP 原生协议支持（从 StdioToHttpProxyService 合并）

    /// <summary>
    /// 创建聚合的MCP服务器选项（用于 MCP 原生协议）
    /// </summary>
    /// <returns>配置好的服务器选项</returns>
    public McpServerOptions CreateAggregatedServerOptions()
    {
        this.EnsureInitialized();

        this._logger.LogInformation(
            "Creating aggregated server with UseNamespacePrefix={UsePrefix}, AllowServerFilter={AllowFilter}",
            this._options.UseNamespacePrefix,
            this._options.AllowServerFilter);

        McpServerOptions options = new()
        {
            ServerInfo = new Implementation
            {
                Name = "mcp-proxy",
                Version = "1.0.0"
            },
            Capabilities = this.GetAggregatedCapabilities(),
            Handlers = new McpServerHandlers()
        };

        // 注册聚合的工具处理器
        options.Handlers.ListToolsHandler = this.CreateListToolsHandler();
        options.Handlers.CallToolHandler = this.CreateCallToolHandler();

        // 注册聚合的提示处理器
        options.Handlers.ListPromptsHandler = this.CreateListPromptsHandler();
        options.Handlers.GetPromptHandler = this.CreateGetPromptHandler();

        // 注册聚合的资源处理器
        options.Handlers.ListResourcesHandler = this.CreateListResourcesHandler();
        options.Handlers.ReadResourceHandler = this.CreateReadResourceHandler();

        return options;
    }

    /// <summary>
    /// 获取所有服务器连接信息（用于管理端点）
    /// </summary>
    public IReadOnlyCollection<(string Name, bool IsConnected, Implementation? ServerInfo, ServerCapabilities? Capabilities)> GetServerConnections()
    {
        this.EnsureInitialized();

        return this._servers.Values
            .Select(s => (s.Name, s.IsConnected, s.ServerInfo, s.Capabilities))
            .ToList();
    }

    #endregion

    #region MCP Handler 创建方法

    private McpRequestHandler<ListToolsRequestParams, ListToolsResult> CreateListToolsHandler()
    {
        return async (request, cancellationToken) =>
        {
            string? serverFilter = this.ExtractServerFilter(request);

            if (serverFilter != null && this._options.AllowServerFilter)
            {
                this._logger.LogDebug("Listing tools for server: {ServerName}", serverFilter);
                return await this.ListToolsFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
            }

            this._logger.LogDebug("Listing tools from all servers");
            return await this.ListAllToolsAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
        };
    }

    private McpRequestHandler<CallToolRequestParams, CallToolResult> CreateCallToolHandler()
    {
        return async (request, cancellationToken) =>
        {
            (string serverName, string toolName) = this.ParseToolName(request.Params?.Name ?? "");

            this._logger.LogDebug(
                "Calling tool '{ToolName}' on server '{ServerName}'",
                toolName,
                serverName);

            if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
            {
                throw new InvalidOperationException($"Server '{serverName}' not found or not connected");
            }

            return await connection.Client.CallToolAsync(
                new CallToolRequestParams
                {
                    Name = toolName,
                    Arguments = request.Params?.Arguments
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        };
    }

    private McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> CreateListPromptsHandler()
    {
        return async (request, cancellationToken) =>
        {
            string? serverFilter = this.ExtractServerFilter(request);

            if (serverFilter != null && this._options.AllowServerFilter)
            {
                return await this.ListPromptsFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
            }

            return await this.ListAllPromptsAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
        };
    }

    private McpRequestHandler<GetPromptRequestParams, GetPromptResult> CreateGetPromptHandler()
    {
        return async (request, cancellationToken) =>
        {
            (string serverName, string promptName) = this.ParseToolName(request.Params?.Name ?? "");

            if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
            {
                throw new InvalidOperationException($"Server '{serverName}' not found or not connected");
            }

            return await connection.Client.GetPromptAsync(
                new GetPromptRequestParams
                {
                    Name = promptName,
                    Arguments = request.Params?.Arguments
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        };
    }

    private McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> CreateListResourcesHandler()
    {
        return async (request, cancellationToken) =>
        {
            string? serverFilter = this.ExtractServerFilter(request);

            if (serverFilter != null && this._options.AllowServerFilter)
            {
                return await this.ListResourcesFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
            }

            return await this.ListAllResourcesAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
        };
    }

    private McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> CreateReadResourceHandler()
    {
        return async (request, cancellationToken) =>
        {
            string uri = request.Params?.Uri ?? "";
            (string serverName, string resourceUri) = this.ParseResourceUri(uri);

            if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
            {
                throw new InvalidOperationException($"Server '{serverName}' not found or not connected");
            }

            return await connection.Client.ReadResourceAsync(
                new ReadResourceRequestParams { Uri = resourceUri },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        };
    }

    private string? ExtractServerFilter(object request)
    {
        if (request == null)
        {
            return null;
        }

        try
        {
            var paramsProperty = request.GetType().GetProperty("Params");
            if (paramsProperty == null) return null;

            var paramsValue = paramsProperty.GetValue(request);
            if (paramsValue == null) return null;

            var metaProperty = paramsValue.GetType().GetProperty("_meta");
            if (metaProperty == null) return null;

            var metaValue = metaProperty.GetValue(paramsValue);
            if (metaValue == null) return null;

            var serverProperty = metaValue.GetType().GetProperty("server");
            if (serverProperty == null) return null;

            return serverProperty.GetValue(metaValue)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 连接到指定的Stdio服务器
    /// </summary>
    private async Task ConnectToServerAsync(McpServerConfig config, CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation(
                "Connecting to server '{ServerName}': {Command} {Arguments}",
                config.Name,
                config.Command,
                string.Join(" ", config.Arguments ?? []));

            StdioClientTransportOptions transportOptions = new()
            {
                Command = config.Command,
                Arguments = config.Arguments,
                EnvironmentVariables = config.Environment as IDictionary<string, string?>,
                WorkingDirectory = config.WorkingDirectory
            };

            StdioClientTransport transport = new(
                transportOptions,
                loggerFactory: this._loggerFactory);

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

            ServerConnection connection = new()
            {
                Name = config.Name,
                Client = client,
                Transport = transport,
                ServerInfo = client.ServerInfo,
                Capabilities = client.ServerCapabilities,
                IsConnected = true,
                LastHeartbeat = DateTime.UtcNow
            };

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

    private (string serverName, string itemName) ParseToolName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            throw new ArgumentException("Item name cannot be empty", nameof(fullName));
        }

        int colonIndex = fullName.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0)
        {
            return (fullName[..colonIndex], fullName[(colonIndex + 1)..]);
        }

        if (this._servers.Count == 1)
        {
            return (this._servers.Keys.First(), fullName);
        }

        throw new InvalidOperationException(
            $"Item name '{fullName}' must include server prefix (format: 'servername:itemname') when multiple servers are configured");
    }

    private (string serverName, string resourceUri) ParseResourceUri(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException("Resource URI cannot be empty", nameof(uri));
        }

        int schemeEnd = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            ReadOnlySpan<char> scheme = uri.AsSpan(0, schemeEnd);
            int colonIndex = scheme.IndexOf(':');

            if (colonIndex > 0)
            {
                string serverName = scheme[..colonIndex].ToString();
                string resourceUri = string.Concat(scheme[(colonIndex + 1)..], uri.AsSpan(schemeEnd));
                return (serverName, resourceUri);
            }
        }

        if (this._servers.Count == 1)
        {
            return (this._servers.Keys.First(), uri);
        }

        throw new InvalidOperationException(
            $"Resource URI '{uri}' must include server prefix when multiple servers are configured");
    }

    private async Task<ListToolsResult> ListToolsFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        ListToolsResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListToolsAsync(param, cancellationToken: ct),
            new ListToolsRequestParams(),
            () => new ListToolsResult { Tools = [] },
            cancellationToken).ConfigureAwait(false);

        if (includePrefix && result.Tools != null)
        {
            foreach (Tool tool in result.Tools)
            {
                tool.Name = $"{serverName}:{tool.Name}";
            }
        }

        return result;
    }

    private async Task<ListToolsResult> ListAllToolsAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        List<Task<ListToolsResult>> tasks = this._servers.Values
            .Select(conn => this.ListToolsFromServerAsync(conn.Name, includePrefix, cancellationToken))
            .ToList();

        ListToolsResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        List<Tool> allTools = results
            .SelectMany(r => r.Tools ?? [])
            .ToList();

        return new ListToolsResult { Tools = allTools };
    }

    private async Task<ListPromptsResult> ListPromptsFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        ListPromptsResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListPromptsAsync(param, cancellationToken: ct),
            new ListPromptsRequestParams(),
            () => new ListPromptsResult { Prompts = [] },
            cancellationToken).ConfigureAwait(false);

        if (includePrefix && result.Prompts != null)
        {
            foreach (Prompt prompt in result.Prompts)
            {
                prompt.Name = $"{serverName}:{prompt.Name}";
            }
        }

        return result;
    }

    private async Task<ListPromptsResult> ListAllPromptsAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        List<Task<ListPromptsResult>> tasks = this._servers.Values
            .Select(conn => this.ListPromptsFromServerAsync(conn.Name, includePrefix, cancellationToken))
            .ToList();

        ListPromptsResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        List<Prompt> allPrompts = results
            .SelectMany(r => r.Prompts ?? [])
            .ToList();

        return new ListPromptsResult { Prompts = allPrompts };
    }

    private async Task<ListResourcesResult> ListResourcesFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        ListResourcesResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListResourcesAsync(param, cancellationToken: ct),
            new ListResourcesRequestParams(),
            () => new ListResourcesResult { Resources = [] },
            cancellationToken).ConfigureAwait(false);

        if (includePrefix && result.Resources != null)
        {
            foreach (Resource resource in result.Resources)
            {
                resource.Uri = AddServerPrefixToUri(serverName, resource.Uri);
            }
        }

        return result;
    }

    private async Task<ListResourcesResult> ListAllResourcesAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        List<Task<ListResourcesResult>> tasks = this._servers.Values
            .Select(conn => this.ListResourcesFromServerAsync(conn.Name, includePrefix, cancellationToken))
            .ToList();

        ListResourcesResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        List<Resource> allResources = results
            .SelectMany(r => r.Resources ?? [])
            .ToList();

        return new ListResourcesResult { Resources = allResources };
    }

    private static string AddServerPrefixToUri(string serverName, string uri)
    {
        int schemeEnd = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            return $"{serverName}:{uri[..schemeEnd]}{uri[schemeEnd..]}";
        }
        return $"{serverName}:{uri}";
    }

    private async Task<TResult> QueryServerSafelyAsync<TParams, TResult>(
        string serverName,
        Func<McpClient, TParams, CancellationToken, ValueTask<TResult>> queryFunc,
        TParams requestParams,
        Func<TResult> defaultResultFactory,
        CancellationToken cancellationToken)
        where TResult : class
    {
        if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
        {
            return defaultResultFactory();
        }

        try
        {
            return await queryFunc(connection.Client, requestParams, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error querying server '{ServerName}'", serverName);
            return defaultResultFactory();
        }
    }

    private void EnsureInitialized()
    {
        if (!this._initialized)
        {
            throw new InvalidOperationException("Service not initialized. Call InitializeAsync first.");
        }
    }

    #endregion

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (ServerConnection connection in this._servers.Values)
        {
            try
            {
                await connection.Client.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error disposing server '{ServerName}'", connection.Name);
            }
        }

        this._servers.Clear();
    }

    private sealed class ServerConnection
    {
        public required string Name { get; init; }
        public required McpClient Client { get; init; }
        public required StdioClientTransport Transport { get; init; }
        public Implementation? ServerInfo { get; init; }
        public ServerCapabilities? Capabilities { get; init; }
        public bool IsConnected { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}

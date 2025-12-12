// Copyright (c) IdeaTech. All rights reserved.

using System.Collections.Concurrent;
using McpProxy.Core.Configuration;
using McpProxy.Abstractions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpProxy.Core.Services;

/// <summary>
/// 实现从Stdio服务器到HTTP的聚合代理服务
/// 该服务连接到一个或多个本地Stdio MCP服务器并通过HTTP/SSE方式聚合暴露
/// </summary>
public sealed class StdioToHttpProxyService : IProxyService
{
    private readonly StdioServersOptions _options;
    private readonly HttpServerOptions _httpOptions;
    private readonly ILogger<StdioToHttpProxyService> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ConcurrentDictionary<string, ServerConnection> _servers = new();

    /// <summary>
    /// 初始化<see cref="StdioToHttpProxyService"/>类的新实例
    /// </summary>
    /// <param name="options">服务器配置选项</param>
    /// <param name="httpOptions">HTTP服务器配置选项</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="loggerFactory">日志工厂（可选）</param>
    public StdioToHttpProxyService(
        IOptions<StdioServersOptions> options,
        IOptions<HttpServerOptions> httpOptions,
        ILogger<StdioToHttpProxyService> logger,
        ILoggerFactory? loggerFactory = null)
    {
        this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this._httpOptions = httpOptions?.Value ?? throw new ArgumentNullException(nameof(httpOptions));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._loggerFactory = loggerFactory;
    }

    /// <summary>
    /// 异步启动Stdio到HTTP的聚合代理服务
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步运行操作的任务</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation(
            "Starting Stdio to HTTP proxy with {ServerCount} servers",
            this._options.Servers.Count);

        // 连接到所有启用的服务器
        List<Task> connectionTasks = new List<Task>();
        foreach (McpServerConfig serverConfig in this._options.Servers.Where(s => s.Enabled))
        {
            Task task = this.ConnectToServerAsync(serverConfig, cancellationToken);
            connectionTasks.Add(task);
        }

        // 等待所有服务器连接完成
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

        // 创建聚合的代理服务器
        McpServerOptions serverOptions = this.CreateAggregatedServerOptions();

        // 构建并运行ASP.NET Core Web应用
        await this.RunWebServerAsync(serverOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 连接到单个Stdio服务器
    /// </summary>
    /// <param name="config">服务器配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ConnectToServerAsync(McpServerConfig config, CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation(
                "Connecting to server '{ServerName}': {Command} {Arguments}",
                config.Name,
                config.Command,
                string.Join(" ", config.Arguments ?? new List<string>()));

            // 创建Stdio客户端传输
            StdioClientTransportOptions transportOptions = new StdioClientTransportOptions
            {
                Command = config.Command,
                Arguments = config.Arguments,
                EnvironmentVariables = config.Environment as IDictionary<string, string?>,
                WorkingDirectory = config.WorkingDirectory
            };

            StdioClientTransport transport = new StdioClientTransport(
                transportOptions,
                loggerFactory: this._loggerFactory);

            // 连接到服务器
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
    /// 创建聚合的代理服务器选项
    /// </summary>
    /// <returns>配置好的服务器选项</returns>
    private McpServerOptions CreateAggregatedServerOptions()
    {
        this._logger.LogInformation(
            "Creating aggregated server with UseNamespacePrefix={UsePrefix}, AllowServerFilter={AllowFilter}",
            this._options.UseNamespacePrefix,
            this._options.AllowServerFilter);

        McpServerOptions options = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = "mcp-proxy",
                Version = "1.0.0"
            },
            Capabilities = this.CreateAggregatedCapabilities(),
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
    /// 创建聚合的服务器能力
    /// </summary>
    private ServerCapabilities CreateAggregatedCapabilities()
    {
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
    /// 创建工具列表处理器
    /// </summary>
    private McpRequestHandler<ListToolsRequestParams, ListToolsResult> CreateListToolsHandler()
    {
        return async (request, cancellationToken) =>
        {
            // 尝试从请求中提取服务器过滤器
            string? serverFilter = this.ExtractServerFilter(request);

            if (serverFilter != null && this._options.AllowServerFilter)
            {
                // 列出特定服务器的工具（不带前缀）
                this._logger.LogDebug("Listing tools for server: {ServerName}", serverFilter);
                return await this.ListToolsFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // 列出所有服务器的工具（带前缀）
                this._logger.LogDebug("Listing tools from all servers");
                return await this.ListAllToolsAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
            }
        };
    }

    /// <summary>
    /// 创建工具调用处理器
    /// </summary>
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

            CallToolResult result = await connection.Client.CallToolAsync(
                new CallToolRequestParams
                {
                    Name = toolName,
                    Arguments = request.Params?.Arguments
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        };
    }

    /// <summary>
    /// 创建提示列表处理器
    /// </summary>
    private McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> CreateListPromptsHandler()
    {
        return async (request, cancellationToken) =>
        {
            string? serverFilter = this.ExtractServerFilter(request);

            if (serverFilter != null && this._options.AllowServerFilter)
            {
                return await this.ListPromptsFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await this.ListAllPromptsAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
            }
        };
    }

    /// <summary>
    /// 创建获取提示处理器
    /// </summary>
    private McpRequestHandler<GetPromptRequestParams, GetPromptResult> CreateGetPromptHandler()
    {
        return async (request, cancellationToken) =>
        {
            (string serverName, string promptName) = this.ParseToolName(request.Params?.Name ?? "");

            if (!this._servers.TryGetValue(serverName, out ServerConnection? connection))
            {
                throw new InvalidOperationException($"Server '{serverName}' not found or not connected");
            }

            GetPromptResult result = await connection.Client.GetPromptAsync(
                new GetPromptRequestParams
                {
                    Name = promptName,
                    Arguments = request.Params?.Arguments
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        };
    }

    /// <summary>
    /// 创建资源列表处理器
    /// </summary>
    private McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> CreateListResourcesHandler()
    {
        return async (request, cancellationToken) =>
        {
            string? serverFilter = this.ExtractServerFilter(request);

            if (serverFilter != null && this._options.AllowServerFilter)
            {
                return await this.ListResourcesFromServerAsync(serverFilter, false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await this.ListAllResourcesAsync(this._options.UseNamespacePrefix, cancellationToken).ConfigureAwait(false);
            }
        };
    }

    /// <summary>
    /// 创建资源读取处理器
    /// </summary>
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

            ReadResourceResult result = await connection.Client.ReadResourceAsync(
                new ReadResourceRequestParams { Uri = resourceUri },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        };
    }

    /// <summary>
    /// 从请求中提取服务器过滤器（如果存在）
    /// </summary>
    private string? ExtractServerFilter(object request)
    {
        if (request == null)
        {
            return null;
        }

        // 尝试通过反射访问 _meta.server 字段
        try
        {
            System.Reflection.PropertyInfo? paramsProperty = request.GetType().GetProperty("Params");
            if (paramsProperty == null)
            {
                return null;
            }

            object? paramsValue = paramsProperty.GetValue(request);
            if (paramsValue == null)
            {
                return null;
            }

            System.Reflection.PropertyInfo? metaProperty = paramsValue.GetType().GetProperty("_meta");
            if (metaProperty == null)
            {
                return null;
            }

            object? metaValue = metaProperty.GetValue(paramsValue);
            if (metaValue == null)
            {
                return null;
            }

            System.Reflection.PropertyInfo? serverProperty = metaValue.GetType().GetProperty("server");
            if (serverProperty == null)
            {
                return null;
            }

            object? serverValue = serverProperty.GetValue(metaValue);
            return serverValue?.ToString();
        }
        catch
        {
            // 如果反射失败，返回 null
            return null;
        }
    }

    /// <summary>
    /// 解析带命名空间前缀的工具名称
    /// </summary>
    /// <param name="fullName">完整的工具名称（可能包含前缀）</param>
    /// <returns>服务器名称和工具名称的元组</returns>
    private (string serverName, string toolName) ParseToolName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            throw new ArgumentException("Tool name cannot be empty", nameof(fullName));
        }

        // 查找冒号分隔符
        int colonIndex = fullName.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex > 0)
        {
            // 格式: "servername:toolname"
            string serverName = fullName[..colonIndex];
            string toolName = fullName[(colonIndex + 1)..];
            return (serverName, toolName);
        }

        // 如果只有一个服务器，直接使用
        if (this._servers.Count == 1)
        {
            return (this._servers.Keys.First(), fullName);
        }

        // 否则抛出异常
        throw new InvalidOperationException($"Tool name '{fullName}' must include server prefix (format: 'servername:toolname') when multiple servers are configured");
    }

    /// <summary>
    /// 解析带命名空间前缀的资源URI
    /// </summary>
    private (string serverName, string resourceUri) ParseResourceUri(string uri)
    {
        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException("Resource URI cannot be empty", nameof(uri));
        }

        // 查找 :// 前的前缀
        int schemeEnd = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            ReadOnlySpan<char> scheme = uri.AsSpan(0, schemeEnd);
            int colonIndex = scheme.IndexOf(':');
            if (colonIndex > 0)
            {
                // 格式: "servername:scheme://path"
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

    /// <summary>
    /// 列出特定服务器的工具
    /// </summary>
    private async Task<ListToolsResult> ListToolsFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        ListToolsResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListToolsAsync(param, cancellationToken: ct),
            new ListToolsRequestParams(),
            () => new ListToolsResult { Tools = new List<Tool>() },
            cancellationToken).ConfigureAwait(false);

        if (includePrefix && result.Tools != null)
        {
            // 添加命名空间前缀
            foreach (Tool tool in result.Tools)
            {
                tool.Name = $"{serverName}:{tool.Name}";
            }
        }

        return result;
    }

    /// <summary>
    /// 列出所有服务器的工具
    /// </summary>
    private async Task<ListToolsResult> ListAllToolsAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        List<Tool> allTools = new List<Tool>();

        // 并发查询所有服务器
        List<Task<ListToolsResult>> tasks = this._servers.Values
            .Select(async conn =>
            {
                return await this.ListToolsFromServerAsync(conn.Name, includePrefix, cancellationToken)
                    .ConfigureAwait(false);
            })
            .ToList();

        ListToolsResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (ListToolsResult result in results)
        {
            if (result.Tools != null)
            {
                allTools.AddRange(result.Tools);
            }
        }

        return new ListToolsResult { Tools = allTools };
    }

    /// <summary>
    /// 列出特定服务器的提示
    /// </summary>
    private async Task<ListPromptsResult> ListPromptsFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        ListPromptsResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListPromptsAsync(param, cancellationToken: ct),
            new ListPromptsRequestParams(),
            () => new ListPromptsResult { Prompts = new List<Prompt>() },
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

    /// <summary>
    /// 列出所有服务器的提示
    /// </summary>
    private async Task<ListPromptsResult> ListAllPromptsAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        List<Prompt> allPrompts = new List<Prompt>();

        List<Task<ListPromptsResult>> tasks = this._servers.Values
            .Select(async conn =>
            {
                return await this.ListPromptsFromServerAsync(conn.Name, includePrefix, cancellationToken)
                    .ConfigureAwait(false);
            })
            .ToList();

        ListPromptsResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (ListPromptsResult result in results)
        {
            if (result.Prompts != null)
            {
                allPrompts.AddRange(result.Prompts);
            }
        }

        return new ListPromptsResult { Prompts = allPrompts };
    }

    /// <summary>
    /// 列出特定服务器的资源
    /// </summary>
    private async Task<ListResourcesResult> ListResourcesFromServerAsync(
        string serverName,
        bool includePrefix,
        CancellationToken cancellationToken)
    {
        ListResourcesResult result = await this.QueryServerSafelyAsync(
            serverName,
            (client, param, ct) => client.ListResourcesAsync(param, cancellationToken: ct),
            new ListResourcesRequestParams(),
            () => new ListResourcesResult { Resources = new List<Resource>() },
            cancellationToken).ConfigureAwait(false);

        if (includePrefix && result.Resources != null)
        {
            foreach (Resource resource in result.Resources)
            {
                // 在URI scheme前添加前缀
                resource.Uri = this.AddServerPrefixToUri(serverName, resource.Uri);
            }
        }

        return result;
    }

    /// <summary>
    /// 列出所有服务器的资源
    /// </summary>
    private async Task<ListResourcesResult> ListAllResourcesAsync(bool includePrefix, CancellationToken cancellationToken)
    {
        List<Resource> allResources = new List<Resource>();

        List<Task<ListResourcesResult>> tasks = this._servers.Values
            .Select(async conn =>
            {
                return await this.ListResourcesFromServerAsync(conn.Name, includePrefix, cancellationToken)
                    .ConfigureAwait(false);
            })
            .ToList();

        ListResourcesResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (ListResourcesResult result in results)
        {
            if (result.Resources != null)
            {
                allResources.AddRange(result.Resources);
            }
        }

        return new ListResourcesResult { Resources = allResources };
    }

    /// <summary>
    /// 在URI添加服务器前缀
    /// </summary>
    private string AddServerPrefixToUri(string serverName, string uri)
    {
        int schemeEnd = uri.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            return $"{serverName}:{uri[..schemeEnd]}{uri[schemeEnd..]}";
        }
        return $"{serverName}:{uri}";
    }

    /// <summary>
    /// 运行Web服务器
    /// </summary>
    private async Task RunWebServerAsync(McpServerOptions serverOptions, CancellationToken cancellationToken)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // 将MCP服务器选项添加到DI容器
        builder.Services.AddSingleton(serverOptions);

        // 添加MCP服务
        builder.Services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.Stateless = this._httpOptions.Stateless;
            });

        // 配置Kestrel服务器
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(this._httpOptions.Port);
        });

        // 配置CORS
        if (this._httpOptions.AllowedOrigins?.Count > 0)
        {
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(this._httpOptions.AllowedOrigins.ToArray())
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
        }

        WebApplication app = builder.Build();

        if (this._httpOptions.AllowedOrigins?.Count > 0)
        {
            app.UseCors();
        }

        // 映射MCP端点
        app.MapMcp();

        // 健康检查端点
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        // 服务器状态端点
        app.MapGet("/servers", () =>
        {
            List<object> servers = this._servers.Values.Select(s => new
            {
                name = s.Name,
                connected = s.IsConnected,
                serverInfo = s.ServerInfo?.Name,
                version = s.ServerInfo?.Version,
                capabilities = new
                {
                    tools = s.Capabilities?.Tools != null,
                    prompts = s.Capabilities?.Prompts != null,
                    resources = s.Capabilities?.Resources != null
                }
            }).ToList<object>();

            return Results.Ok(new { servers = servers, count = servers.Count });
        });

        this._logger.LogInformation(
            "HTTP server listening on http://{Host}:{Port}",
            this._httpOptions.Host,
            this._httpOptions.Port);

        await app.RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 通用的服务器查询辅助方法，带异常处理
    /// </summary>
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

    /// <summary>
    /// 服务器连接信息
    /// </summary>
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

// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy;

/// <summary>
/// Stdio 到 Streamable HTTP 代理服务的基类，提供通用功能，包括资源释放模式和客户端选项创建
/// 此基类实现了 IMcpProxyService 接口，为不同的 MCP 代理服务提供统一的基础实现
/// </summary>
/// <param name="logger">日志记录器实例</param>
public abstract class BaseStdioToHttpProxyService(ILogger logger) : IMcpProxyService
{
    /// <summary>
    /// 获取日志记录器实例
    /// </summary>
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 获取缓存的空JSON对象，以避免重复解析
    /// </summary>
    protected static readonly JsonElement EmptyJsonObject;

    /// <summary>
    /// 标识对象是否已被释放
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 静态构造函数，初始化空JSON对象
    /// </summary>
    static BaseStdioToHttpProxyService()
    {
        // 创建一个空的JSON对象并克隆以供重用
        using JsonDocument doc = JsonDocument.Parse("{}");
        EmptyJsonObject = doc.RootElement.Clone();
    }

    // ========== Tools 抽象方法 ==========

    /// <inheritdoc />
    public abstract ValueTask<ListToolsResult> ListToolsAsync(
        string? mcpServerName = null,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async ValueTask<ListToolsResult> ListToolsAsync(
        RequestContext<ListToolsRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文提取参数并调用核心实现
        return await this.ListToolsAsync(
            mcpServerName: null,
            cursor: request.Params?.Cursor,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<ListToolsResult> ListToolsAsync(
        ListToolsRequestParams listToolsRequestParams,
        CancellationToken cancellationToken = default)
    {
        // 从参数对象提取参数并调用核心实现
        return await this.ListToolsAsync(
            mcpServerName: null,
            cursor: listToolsRequestParams?.Cursor,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public abstract ValueTask<CallToolResult> CallToolAsync(
        CallToolRequestParams callToolRequestParams,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async ValueTask<CallToolResult> CallToolAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文中提取参数并调用重载方法
        return await this.CallToolAsync(request.Params!, cancellationToken).ConfigureAwait(false);
    }

    // ========== Prompts 默认实现（抛出未实现异常） ==========

    /// <inheritdoc />
    public virtual ValueTask<ListPromptsResult> ListPromptsAsync(
        string? mcpServerName = null,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Prompts功能尚未在此处理器中实现");
    }

    /// <inheritdoc />
    public virtual async ValueTask<ListPromptsResult> ListPromptsAsync(
        RequestContext<ListPromptsRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文提取参数并调用核心实现
        return await this.ListPromptsAsync(
            mcpServerName: null,
            cursor: request.Params?.Cursor,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<ListPromptsResult> ListPromptsAsync(
        ListPromptsRequestParams listPromptsRequestParams,
        CancellationToken cancellationToken = default)
    {
        // 从参数对象提取参数并调用核心实现
        return await this.ListPromptsAsync(
            mcpServerName: null,
            cursor: listPromptsRequestParams?.Cursor,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<GetPromptResult> GetPromptAsync(
        RequestContext<GetPromptRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文中提取参数并调用重载方法
        return await this.GetPromptAsync(request.Params!, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual ValueTask<GetPromptResult> GetPromptAsync(
        GetPromptRequestParams getPromptRequestParams,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Prompts功能尚未在此处理器中实现");
    }

    // ========== Resources 默认实现（抛出未实现异常） ==========

    /// <inheritdoc />
    public virtual ValueTask<ListResourcesResult> ListResourcesAsync(
        string? mcpServerName = null,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Resources功能尚未在此处理器中实现");
    }

    /// <inheritdoc />
    public virtual async ValueTask<ListResourcesResult> ListResourcesAsync(
        RequestContext<ListResourcesRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文提取参数并调用核心实现
        return await this.ListResourcesAsync(
            mcpServerName: null,
            cursor: request.Params?.Cursor,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<ListResourcesResult> ListResourcesAsync(
        ListResourcesRequestParams listResourcesRequestParams,
        CancellationToken cancellationToken = default)
    {
        // 从参数对象提取参数并调用核心实现
        return await this.ListResourcesAsync(
            mcpServerName: null,
            cursor: listResourcesRequestParams?.Cursor,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async ValueTask<ReadResourceResult> ReadResourceAsync(
        RequestContext<ReadResourceRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文中提取参数并调用重载方法
        return await this.ReadResourceAsync(request.Params!, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual ValueTask<ReadResourceResult> ReadResourceAsync(
        ReadResourceRequestParams readResourceRequestParams,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Resources功能尚未在此处理器中实现");
    }

    /// <inheritdoc />
    public virtual async ValueTask SubscribeResourceAsync(
        RequestContext<SubscribeRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文中提取参数并调用重载方法
        await this.SubscribeResourceAsync(request.Params!, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual ValueTask SubscribeResourceAsync(
        SubscribeRequestParams subscribeRequestParams,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Resources订阅功能尚未在此处理器中实现");
    }

    /// <inheritdoc />
    public virtual async ValueTask UnsubscribeResourceAsync(
        RequestContext<UnsubscribeRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        // 从请求上下文中提取参数并调用重载方法
        await this.UnsubscribeResourceAsync(request.Params!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 取消订阅资源更新通知（使用参数对象）
    /// </summary>
    /// <param name="unsubscribeRequestParams">取消订阅请求参数，包含要取消订阅的资源URI</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步操作的任务</returns>
    public virtual ValueTask UnsubscribeResourceAsync(
        UnsubscribeRequestParams unsubscribeRequestParams,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Resources取消订阅功能尚未在此处理器中实现");
    }

    // ========== 生命周期管理（抽象方法）==========

    /// <summary>
    /// 刷新服务缓存
    /// 派生类必须实现此方法以重新发现服务器并重建映射
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步刷新操作的任务</returns>
    public abstract Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务状态信息
    /// 派生类必须实现此方法以返回当前状态
    /// </summary>
    /// <returns>服务状态信息对象</returns>
    public abstract ServiceStatus GetStatus();

    /// <summary>
    /// 验证所有服务器连接的健康状态
    /// 派生类必须实现此方法以检查服务器健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    public abstract Task<HealthCheckResult> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 释放对象使用的所有资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // 防止重复释放
        if (this._disposed)
        {
            return;
        }

        try
        {
            // 调用派生类的清理逻辑
            await this.DisposeAsyncCore().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // 记录释放过程中的错误，但不抛出异常
            this._logger.LogError(ex, "Error occurred while disposing proxy service {ServiceType}. Some resources may not have been properly disposed.", this.GetType().Name);
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
        // 默认实现不执行任何操作
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 使用指定的MCP服务器配置创建客户端选项
    /// </summary>
    /// <param name="server">MCP服务器实例</param>
    /// <returns>配置好的MCP客户端选项</returns>
    protected McpClientOptions CreateClientOptions(McpServer server)
    {
        // 创建客户端处理器集合
        McpClientHandlers handlers = new();

        // 如果服务器支持采样功能，则配置采样处理器
        if (server.ClientCapabilities?.Sampling is not null)
        {
            handlers.SamplingHandler = (request, progress, token) =>
            {
                ArgumentNullException.ThrowIfNull(request, nameof(request));
                return server.SampleAsync(request, token);
            };
        }

        // 如果服务器支持引导功能，则配置引导处理器
        if (server.ClientCapabilities?.Elicitation is not null)
        {
            handlers.ElicitationHandler = (request, token) =>
            {
                ArgumentNullException.ThrowIfNull(request, nameof(request));
                return server.ElicitAsync(request, token);
            };
        }

        // 创建并返回客户端选项
        McpClientOptions clientOptions = new()
        {
            ClientInfo = server.ClientInfo,
            Handlers = handlers,
        };

        return clientOptions;
    }

    /// <summary>
    /// 使用指定的基本信息创建客户端选项
    /// </summary>
    /// <param name="name">客户端名称</param>
    /// <param name="title">客户端标题，可选</param>
    /// <param name="version">客户端版本号，默认为"1.0.0"</param>
    /// <returns>配置好的MCP客户端选项</returns>
    protected McpClientOptions CreateClientOptions(string name, string? title = null, string version = "1.0.0")
    {
        // 创建实现信息对象
        Implementation implementation = new()
        {
            Name = name,
            Version = version,
            Title = title
        };

        // 创建并返回客户端选项
        McpClientOptions clientOptions = new()
        {
            ClientInfo = implementation
        };

        return clientOptions;
    }
}

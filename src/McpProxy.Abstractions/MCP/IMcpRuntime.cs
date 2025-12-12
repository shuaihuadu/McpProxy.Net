namespace McpProxy;

/// <summary>
/// 定义模型上下文协议（MCP）运行时的核心功能
/// 运行时负责处理工具发现和调用、提示词管理、资源访问等请求
/// </summary>
public interface IMcpRuntime : IAsyncDisposable
{
    // ========== Tools Handlers ==========

    /// <summary>
    /// 处理列出MCP服务器中所有可用工具的请求
    /// </summary>
    /// <param name="request">包含元数据和参数的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>包含可用工具列表的结果对象</returns>
    ValueTask<ListToolsResult> ListToolsHandler(RequestContext<ListToolsRequestParams>? request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理使用提供的上下文调用特定工具的请求
    /// </summary>
    /// <param name="request">包含工具名称和参数的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>包含工具调用输出的结果对象</returns>
    ValueTask<CallToolResult> CallToolHandler(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理使用提供的参数调用特定工具的请求
    /// </summary>
    /// <param name="callToolRequestParams">调用工具的请求参数</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>包含工具调用输出的结果对象</returns>
    ValueTask<CallToolResult> CallToolHandler(CallToolRequestParams callToolRequestParams, CancellationToken cancellationToken = default);

    // ========== Prompts Handlers ==========

    /// <summary>
    /// 处理列出MCP服务器中所有可用提示词的请求
    /// </summary>
    /// <param name="request">包含元数据和参数的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>包含可用提示词列表的结果对象</returns>
    ValueTask<ListPromptsResult> ListPromptsHandler(RequestContext<ListPromptsRequestParams>? request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理获取特定提示词内容的请求
    /// </summary>
    /// <param name="request">包含提示词名称和参数的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>包含提示词内容的结果对象</returns>
    ValueTask<GetPromptResult> GetPromptHandler(RequestContext<GetPromptRequestParams> request, CancellationToken cancellationToken = default);

    // ========== Resources Handlers ==========

    /// <summary>
    /// 处理列出MCP服务器中所有可用资源的请求
    /// </summary>
    /// <param name="request">包含元数据和参数的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>包含可用资源列表的结果对象</returns>
    ValueTask<ListResourcesResult> ListResourcesHandler(RequestContext<ListResourcesRequestParams>? request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理读取特定资源内容的请求
    /// </summary>
    /// <param name="request">包含资源URI的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>包含资源内容的结果对象</returns>
    ValueTask<ReadResourceResult> ReadResourceHandler(RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理订阅资源更新通知的请求
    /// </summary>
    /// <param name="request">包含要订阅的资源URI的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>表示异步操作的任务</returns>
    ValueTask SubscribeResourceHandler(RequestContext<SubscribeRequestParams> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理取消订阅资源更新通知的请求
    /// </summary>
    /// <param name="request">包含要取消订阅的资源URI的请求上下文</param>
    /// <param name="cancellationToken">用于监视取消请求的令牌</param>
    /// <returns>表示异步操作的任务</returns>
    ValueTask UnsubscribeResourceHandler(RequestContext<UnsubscribeRequestParams> request, CancellationToken cancellationToken = default);

    // ========== 生命周期管理 ==========

    /// <summary>
    /// 刷新底层代理服务的缓存
    /// 委托给 IMcpProxyService.RefreshAsync()
    /// </summary>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示异步刷新操作的任务</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务状态信息
    /// 委托给 IMcpProxyService.GetStatus()
    /// </summary>
    /// <returns>服务状态信息对象</returns>
    ServiceStatusInfo GetStatus();

    /// <summary>
    /// 验证所有服务器连接的健康状态
    /// 委托给 IMcpProxyService.ValidateAsync()
    /// </summary>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>健康检查结果</returns>
    Task<HealthCheckResult> ValidateAsync(CancellationToken cancellationToken = default);
}

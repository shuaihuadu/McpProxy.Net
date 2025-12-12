namespace McpProxy;

/// <summary>
/// 定义模型上下文协议（MCP）运行时的核心功能
/// 运行时负责处理工具发现和调用请求
/// </summary>
public interface IMcpRuntime : IAsyncDisposable
{
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
}

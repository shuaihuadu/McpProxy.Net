namespace McpProxy;

/// <summary>
/// 定义了MCP服务器中工具操作相关的接口，主要负责发现、列出和调用工具
/// </summary>
public interface IMcpToolsHandler : IAsyncDisposable
{
    /// <summary>
    /// 处理列出MCP服务器中所有可用工具的请求
    /// </summary>
    /// <param name="mcpServerName">需要列出工具的MCP服务器名称。如果为空或null，则列出所有服务器的工具</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>包含工具列表的结果对象</returns>
    ValueTask<ListToolsResult> ListToolsAsync(string? mcpServerName = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理调用MCP服务器中特定工具的请求（使用请求上下文）
    /// </summary>
    /// <param name="request">调用工具的请求上下文，包含工具名称和参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>工具调用的结果对象，包含执行结果或错误信息</returns>
    ValueTask<CallToolResult> CallToolsAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理调用MCP服务器中特定工具的请求（使用参数对象）
    /// </summary>
    /// <param name="callToolRequestParams">调用工具的参数对象，包含工具名称和参数</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>工具调用的结果对象，包含执行结果或错误信息</returns>
    ValueTask<CallToolResult> CallToolsAsync(CallToolRequestParams? callToolRequestParams, CancellationToken cancellationToken = default);
}

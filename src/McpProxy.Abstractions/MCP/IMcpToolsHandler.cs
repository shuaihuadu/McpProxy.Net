namespace McpProxy;

/// <summary>
/// 定义了MCP服务器中Tool操作相关的接口，主要负责发现、列出和调用工具
/// </summary>
public interface IMcpToolsHandler : IAsyncDisposable
{
    /// <summary>
    /// 处理列出MCP服务器中所有可用工具的请求
    /// </summary>
    /// <param name="mcpServerName">需要列出的工具的MCP服务器</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask<ListToolsResult> ListToolsAsync(string? mcpServerName = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理调用MCP服务器中特定工具的请求
    /// </summary>
    /// <param name="request">调用工具的请求上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask<CallToolResult> CallToolsAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理调用MCP服务器中特定工具的请求
    /// </summary>
    /// <param name="callToolRequestParams">调用工具的参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask<CallToolResult> CallToolsAsync(CallToolRequestParams? callToolRequestParams, CancellationToken cancellationToken = default);
}

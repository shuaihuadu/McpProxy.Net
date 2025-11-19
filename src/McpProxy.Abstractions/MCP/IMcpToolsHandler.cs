namespace McpProxy;

public interface IMcpToolsHandler : IAsyncDisposable
{
    ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> requestContext, CancellationToken cancellationToken = default);

    ValueTask<CallToolResult> CallToolsAsync(RequestContext<CallToolRequestParams> requestContext, CancellationToken cancellationToken = default);
}

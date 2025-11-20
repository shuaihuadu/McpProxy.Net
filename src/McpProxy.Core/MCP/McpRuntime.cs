namespace McpProxy;

/// <summary>
/// Implementation of the MCP runtime.
/// Provides logging and configuration support for the MCP server.
/// </summary>
public class McpRuntime : IMcpRuntime
{
    private readonly IMcpToolsHandler _toolsHandler;
    private readonly ILogger<McpRuntime> _logger;

    public McpRuntime(IMcpToolsHandler toolsHandler, ILogger<McpRuntime> logger)
    {
        this._toolsHandler = toolsHandler;
        this._logger = logger;

        _logger.LogInformation("McpRuntime initialized with tool loader of type {ToolHandlerType}.", _toolsHandler.GetType().Name);
    }

    /// <inheritdoc />
    public async ValueTask<ListToolsResult> ListToolsHandler(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken = default)
    {
        try
        {
            ListToolsResult result = await this._toolsHandler.ListToolsAsync(request, cancellationToken);

            return result;
        }
        catch (Exception)
        {
            // TODO 可观测性处理
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask<CallToolResult> CallToolHandler(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        if (request.Params == null)
        {
            var content = new TextContentBlock
            {
                Text = "Cannot call tools with null parameters.",
            };

            return new CallToolResult
            {
                Content = [content],
                IsError = true,
            };
        }

        try
        {
            return await this._toolsHandler.CallToolsAsync(request, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock
                {
                    Text = ex.Message,
                }],
                IsError = true,
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Disposes the tool loader and releases associated resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await this._toolsHandler.DisposeAsync();
    }
}
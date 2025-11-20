namespace McpProxy;

/// <summary>
/// MCP Tools处理的的基类，提供通用功能，包括释放模式。
/// </summary>
/// <param name="logger">日志记录器</param>
public abstract class BaseMcpToolsHandler(ILogger logger) : IMcpToolsHandler
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 缓存的空JSON对象以避免重复解析
    /// </summary>
    protected static readonly JsonElement EmptyJsonObject;

    static BaseMcpToolsHandler()
    {
        using JsonDocument doc = JsonDocument.Parse("{}");

        EmptyJsonObject = doc.RootElement.Clone();
    }

    private bool _disposed = false;

    /// <inheritdoc/>
    public abstract ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract ValueTask<CallToolResult> CallToolsAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes resources owned by this tool loader with double disposal protection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await DisposeAsyncCore();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while disposing tool loader {LoaderType}. Some resources may not have been properly disposed.", GetType().Name);
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Override this method in derived classes to implement disposal logic.
    /// This method is called exactly once during disposal.
    /// </summary>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates a new instance of <see cref="McpClientOptions"/> configured with handlers and client information based
    /// on the specified server's capabilities.
    /// </summary>
    /// <remarks>The returned client options will include handlers for sampling and elicitation if the
    /// corresponding capabilities are present in the server. Handlers are set to invoke the server's asynchronous
    /// methods for sampling and elicitation as appropriate.</remarks>
    /// <param name="server">The server from which client capabilities and information are retrieved to configure the client options. Cannot
    /// be null.</param>
    /// <returns>A <see cref="McpClientOptions"/> instance containing handlers and client information derived from the provided
    /// server.</returns>
    protected McpClientOptions CreateClientOptions(McpServer server)
    {
        McpClientHandlers handlers = new();

        if (server.ClientCapabilities?.Sampling is not null)
        {
            handlers.SamplingHandler = (request, progress, token) =>
            {
                ArgumentNullException.ThrowIfNull(request, nameof(request));

                return server.SampleAsync(request, token);
            };
        }

        if (server.ClientCapabilities?.Elicitation is not null)
        {
            handlers.ElicitationHandler = (request, token) =>
            {
                ArgumentNullException.ThrowIfNull(request, nameof(request));
                return server.ElicitAsync(request, token);
            };
        }

        McpClientOptions clientOptions = new()
        {
            ClientInfo = server.ClientInfo,
            Handlers = handlers,
        };

        return clientOptions;
    }
}
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
    /// 释放资源
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
    /// 覆盖此方法以在派生类中实现特定的清理逻辑
    /// 这个方法在释放过程中仅被调用一次
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 使用指定的MCP服务器创建相关的客户端配置信息
    /// </summary>
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

    /// <summary>
    /// 使用指定的信息创建相关的客户端配置信息
    /// </summary>
    protected McpClientOptions CreateClientOptions(string name, string? title = null, string version = "1.0.0")
    {
        Implementation implementation = new()
        {
            Name = name,
            Version = version,
            Title = title
        };

        McpClientOptions clientOptions = new()
        {
            ClientInfo = implementation
        };

        return clientOptions;
    }
}
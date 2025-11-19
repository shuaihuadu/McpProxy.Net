using Microsoft.Extensions.DependencyInjection;

namespace McpProxy;

public abstract class BaseMcpToolsHandler : IMcpToolsHandler
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    protected static readonly JsonElement EmptyJsonObject;

    static BaseMcpToolsHandler()
    {
        using JsonDocument doc = JsonDocument.Parse("{}");

        EmptyJsonObject = doc.RootElement.Clone();
    }

    protected BaseMcpToolsHandler(IServiceProvider services, ILogger logger)
    {
        this._services = services;
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private bool _disposed = false;

    public abstract ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken = default);

    public abstract ValueTask<CallToolResult> CallToolsAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default);

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

    protected virtual ValueTask DisposeAsyncCore()
    {
        return ValueTask.CompletedTask;
    }

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

    protected static JsonElement GetParametersJsonElement(RequestContext<CallToolRequestParams> request)
    {
        IReadOnlyDictionary<string, JsonElement>? args = request.Params?.Arguments;

        if (args is not null && args.TryGetValue("parameters", out var parametersElement) && parametersElement.ValueKind == JsonValueKind.Object)
        {
            return parametersElement;
        }

        return EmptyJsonObject;
    }

    protected static Dictionary<string, object?> GetParametersDictionary(RequestContext<CallToolRequestParams> request)
    {
        JsonElement parameterElement = GetParametersJsonElement(request);

        return parameterElement.EnumerateObject().ToDictionary(prop => prop.Name, prop => (object?)prop.Value);
    }

    /// <inheritdoc/>
    protected async Task<IMcpServerProvider> FindServerProviderAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name), "Server name cannot be null or empty.");
        }

        IEnumerable<IMcpServerProvider> serverProviders = this._services.GetServices<IMcpServerProvider>();

        foreach (var serverProvider in serverProviders)
        {
            var metadata = serverProvider.CreateMetadata();
            if (metadata.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return serverProvider;
            }
        }

        throw new KeyNotFoundException($"No MCP server found with the name '{name}'.");
    }
}
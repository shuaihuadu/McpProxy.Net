namespace McpProxy;

public class NamedMcpServerProvider : IMcpServerProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NamedMcpServerProvider> _logger;
    private readonly Dictionary<string, NamedMcpServerOptions> _options;
    protected readonly Dictionary<string, McpClient> _clientCache = new(StringComparer.OrdinalIgnoreCase);

    public NamedMcpServerProvider(ILoggerFactory loggerFactory, IOptions<Dictionary<string, NamedMcpServerOptions>> options)
    {
        this._loggerFactory = loggerFactory;
        this._logger = loggerFactory.CreateLogger<NamedMcpServerProvider>();
        this._options = options.Value;
    }

    public IEnumerable<McpServerMetadata> CreateMetadata()
    {
        IList<McpServerMetadata> mcpServerMetadata = [];

        foreach (var item in this._options)
        {
            McpServerMetadata metadata = new()
            {
                Id = item.Key,
                Name = item.Key,
                Title = item.Key,
                Description = item.Key
            };
            mcpServerMetadata.Add(metadata);
        }

        return mcpServerMetadata;
    }

    public async Task<McpClient> GetOrCreateClientAsync(string name, McpClientOptions clientOptions, CancellationToken cancellationToken = default)
    {
        NamedMcpServerOptions? serverOptions = this._options.FirstOrDefault(s => string.Equals(s.Key, name, StringComparison.OrdinalIgnoreCase)).Value
            ?? throw new InvalidOperationException($"The named server {name} not found in mcp.json");

        // TODO HTTP / SSE TransportOptions support

        StdioClientTransportOptions? transportOptions = serverOptions.ToStdioClientTransportOptions(name)
            ?? throw new InvalidOperationException($"Invalid configuration for server {name}");

        StdioClientTransport clientTransport = new(transportOptions, this._loggerFactory);

        return await McpClient.CreateAsync(clientTransport, clientOptions, this._loggerFactory, cancellationToken).ConfigureAwait(false);
    }
}

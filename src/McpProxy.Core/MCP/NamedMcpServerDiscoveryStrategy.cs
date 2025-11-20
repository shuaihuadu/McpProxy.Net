namespace McpProxy;

/// <summary>
/// Discovers MCP servers from an mcp.json configuration file.
/// This strategy loads server configurations from mcp.json configuration file.
/// </summary>
/// <param name="options">Options for configuring the service behavior.</param>
/// <param name="logger">Logger instance for this discovery strategy.</param>
public sealed class NamedMcpServerDiscoveryStrategy(IOptions<NamedMcpServersOptions> options, ILogger<NamedMcpServerDiscoveryStrategy> logger) : BaseDiscoveryStrategy(logger)
{
    private readonly IOptions<NamedMcpServersOptions> _options = options;

    /// <inheritdoc/>
    public override Task<IEnumerable<IMcpServerProvider>> DiscoverServersAsync(CancellationToken cancellationToken)
    {
        IEnumerable<IMcpServerProvider> servers = this._options.Value.Servers!.Select(server => new NamedMcpServerProvider(server.Key, server.Value))
           .Cast<IMcpServerProvider>();

        return Task.FromResult(servers);
    }
}

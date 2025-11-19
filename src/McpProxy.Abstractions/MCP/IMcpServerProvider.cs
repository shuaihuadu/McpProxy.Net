namespace McpProxy;

public interface IMcpServerProvider
{
    IEnumerable<McpServerMetadata> CreateMetadata();

    Task<McpClient> GetOrCreateClientAsync(string name, McpClientOptions clientOptions, CancellationToken cancellationToken = default);
}

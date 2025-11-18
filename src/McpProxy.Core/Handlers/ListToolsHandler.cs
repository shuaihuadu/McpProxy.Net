using McpProxy.Models;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpProxy;

public class ListToolsHandler
{
    public async ValueTask<ListToolsResult> ListToolsAsync(StdioServerOptions stdioServerOptions, CancellationToken cancellationToken = default)
    {
        StdioClientTransportOptions stdioClientTransportOptions = stdioServerOptions.ToStdioClientTransportOptions();

        await using McpClient client = await McpClient.CreateAsync(new StdioClientTransport(stdioClientTransportOptions), cancellationToken: cancellationToken);

        IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        return new ListToolsResult
        {
            Tools = [.. tools.Select(t => t.ProtocolTool)],
        };
    }
}

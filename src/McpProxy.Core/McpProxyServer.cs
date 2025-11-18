using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpProxy.Core;

public class McpProxyServer
{
    public async Task<McpServer> CreateMcpProxyServerAsync(McpClient client)
    {
        Implementation implementation = new() { Name = client.ServerInfo.Name, Version = client.ServerInfo.Version };

        McpServerHandlers handlers = new()
        {
            ListToolsHandler = async (request, cancellationToken) =>
            {
                IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                return new ListToolsResult
                {
                    Tools = [.. tools.Select(t => t.ProtocolTool)]
                };
            }
        };

        //McpServerOptions options = new()
        //{
        //    ServerInfo = implementation,
        //    Handlers = handlers
        //};

        McpServer server = McpServer.Create(new StdioServerTransport("MyServer"), default);

        return server;
    }
}

using McpProxy;

var builder = WebApplication.CreateBuilder(args);

// Add mcp server configuration
builder.Configuration.AddJsonFile("mcp.json", optional: false, reloadOnChange: true);

builder.Services.AddMcpServer(options =>
{
    options.Handlers.ListToolsHandler = async (request, cancellationToken) =>
    {
        ListToolsHandler listToolsHandler = new ListToolsHandler();

        return await listToolsHandler.ListToolsAsync(new McpProxy.Models.StdioServerOptions
        {
            Name = "fetch",
            Command = "uvx",
            Arguments = ["mcp-server-fetch"]
        }, cancellationToken);
    };

}).WithHttpTransport();

//builder.Services.AddOpenTelemetry();

var app = builder.Build();

app.MapMcp("mcp");

app.Run();
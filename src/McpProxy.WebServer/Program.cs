using McpProxy;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add mcp server configuration
builder.Configuration.AddJsonFile("mcp.json", optional: false, reloadOnChange: true);

builder.Services.AddSingleton<IMcpToolsHandler, ServerToolsHandler>();

builder.Services.AddSingleton<IMcpServerDiscoveryStrategy, InFileNamedMcpServerDiscoveryStrategy>();

builder.Services.AddSingleton<IMcpRuntime, McpRuntime>();

builder.Services.Configure<NamedMcpServersOptions>(builder.Configuration);

builder.Services.AddOptions<McpServerOptions>().Configure<IMcpRuntime>((mcpServerOptions, mcpRuntime) =>
{
    var entryAssembly = Assembly.GetEntryAssembly();
    var assemblyName = entryAssembly?.GetName();
    var serverName = entryAssembly?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Mcp Proxy Server";

    mcpServerOptions.ProtocolVersion = "2024-11-05";
    mcpServerOptions.ServerInfo = new Implementation
    {
        Name = serverName,
        Version = assemblyName?.Version?.ToString() ?? "1.0.0"
    };

    mcpServerOptions.Handlers = new()
    {
        CallToolHandler = mcpRuntime.CallToolHandler,
        ListToolsHandler = mcpRuntime.ListToolsHandler
    };

    // TODO: Load from configuration or external source
    mcpServerOptions.ServerInstructions = "Expose the stdio mcp server to http";
});

builder.Services.AddMcpServer().WithHttpTransport();

//builder.Services.AddOpenTelemetry();

var app = builder.Build();

app.MapMcp("mcp");

app.Run();
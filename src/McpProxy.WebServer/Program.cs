// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Reflection;
using McpProxy;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Add mcp server configuration
builder.Configuration.AddJsonFile("mcp.json", optional: false, reloadOnChange: true);

// Register MCP proxy service
builder.Services.AddSingleton<IMcpProxyService, StdioToHttpProxyService>();

// Register MCP server discovery strategy
builder.Services.AddSingleton<IMcpServerDiscoveryStrategy, ConfigurationServerDiscoveryStrategy>();

// Configure stdio MCP server options
builder.Services.Configure<StdioMcpServersOptions>(builder.Configuration);

builder.Services.AddOptions<McpServerOptions>().Configure<IMcpProxyService>((mcpServerOptions, proxyService) =>
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
        CallToolHandler = (request, cancellationToken) => proxyService.CallToolAsync(request, cancellationToken),

        ListToolsHandler = (request, cancellationToken) => proxyService.ListToolsAsync(request, cancellationToken),

        GetPromptHandler = (request, cancellationToken) => proxyService.GetPromptAsync(request, cancellationToken),

        ListPromptsHandler = (request, cancellationToken) => proxyService.ListPromptsAsync(request, cancellationToken),

        ListResourcesHandler = (request, cancellationToken) => proxyService.ListResourcesAsync(request, cancellationToken),

        ReadResourceHandler = (request, cancellationToken) => proxyService.ReadResourceAsync(request, cancellationToken),
    };

    // TODO: Load from configuration or external source
    mcpServerOptions.ServerInstructions = "Expose the stdio mcp server to http";
});

builder.Services.AddMcpServer().WithHttpTransport();

//builder.Services.AddOpenTelemetry();

var app = builder.Build();

app.MapMcp("mcp");

app.Run();

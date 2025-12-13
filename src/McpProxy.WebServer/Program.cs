// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Reflection;
using McpProxy;
using McpProxy.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Add mcp server configuration
builder.Configuration.AddJsonFile("mcp.json", optional: false, reloadOnChange: true);

// Register MCP proxy service
builder.Services.AddSingleton<IMcpProxyService, StdioToHttpProxyService>();

// Register MCP server discovery strategy
builder.Services.AddSingleton<IMcpServerDiscoveryStrategy, ConfigurationServerDiscoveryStrategy>();

// Register background service for pre-initialization
builder.Services.AddHostedService<McpProxyInitializationService>();

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
        // Tools - 直接调用 IMcpProxyService 的 RequestContext 重载
        CallToolHandler = (request, cancellationToken) =>
            proxyService.CallToolAsync(request, cancellationToken),

        ListToolsHandler = (request, cancellationToken) =>
            proxyService.ListToolsAsync(request, cancellationToken),

        // Prompts - 直接调用 IMcpProxyService 的 RequestContext 重载
        GetPromptHandler = (request, cancellationToken) =>
            proxyService.GetPromptAsync(request, cancellationToken),

        ListPromptsHandler = (request, cancellationToken) =>
            proxyService.ListPromptsAsync(request, cancellationToken),

        // Resources - 直接调用 IMcpProxyService 的 RequestContext 重载
        ListResourcesHandler = (request, cancellationToken) =>
            proxyService.ListResourcesAsync(request, cancellationToken),

        ReadResourceHandler = (request, cancellationToken) =>
            proxyService.ReadResourceAsync(request, cancellationToken),
    };

    // TODO: Load from configuration or external source
    mcpServerOptions.ServerInstructions = "Expose the stdio mcp server to http";
});

builder.Services.AddMcpServer().WithHttpTransport();

// Add Controllers and API Explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger for API documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MCP Proxy Management API",
        Version = "v1",
        Description = "MCP 服务器管理和监控 REST API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "McpProxy.Net",
            Url = new Uri("https://github.com/shuaihuadu/McpProxy.Net")
        }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

//builder.Services.AddOpenTelemetry();

var app = builder.Build();

// Configure Swagger UI (available in all environments for debugging)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Proxy Management API v1");
    options.RoutePrefix = "swagger"; // Swagger UI at /swagger
    options.DocumentTitle = "MCP Proxy Management API";
});

// Map MCP endpoint
app.MapMcp("mcp");

// Map Controllers
app.MapControllers();

app.Run();

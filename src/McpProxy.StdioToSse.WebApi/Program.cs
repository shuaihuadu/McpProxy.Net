// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Services;
using McpProxy.Core.Configuration;
using McpProxy.Core.Services;
using McpProxy.StdioToSse.WebApi.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ==================== 配置加载 ====================

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables(prefix: "MCPPROXY_")
    .AddCommandLine(args);

// ==================== MCP 服务器配置 ====================

List<McpServerConfig>? mcpServers = builder.Configuration
    .GetSection("McpServers")
    .Get<List<McpServerConfig>>();

if (mcpServers == null || mcpServers.Count == 0)
{
    throw new InvalidOperationException(
        "At least one MCP server must be configured in 'McpServers' section");
}

// 自动为未命名的服务器分配名称
for (int i = 0; i < mcpServers.Count; i++)
{
    McpServerConfig server = mcpServers[i];
    if (string.IsNullOrEmpty(server.Name))
    {
        server.Name = mcpServers.Count == 1 ? "default" : $"server{i + 1}";
    }
}

// 构建 StdioServersOptions
bool useNamespacePrefix = builder.Configuration.GetValue("UseNamespacePrefix", true);
bool allowServerFilter = builder.Configuration.GetValue("AllowServerFilter", true);
bool autoReconnect = builder.Configuration.GetValue("AutoReconnect", true);
int healthCheckInterval = builder.Configuration.GetValue("HealthCheckInterval", 30);

StdioServersOptions stdioOptions = new()
{
    Servers = mcpServers,
    UseNamespacePrefix = mcpServers.Count > 1 && useNamespacePrefix,
    AllowServerFilter = allowServerFilter,
    AutoReconnect = autoReconnect,
    HealthCheckInterval = healthCheckInterval
};

// 配置选项
builder.Services.Configure<StdioServersOptions>(options =>
{
    options.Servers = stdioOptions.Servers;
    options.UseNamespacePrefix = stdioOptions.UseNamespacePrefix;
    options.AllowServerFilter = stdioOptions.AllowServerFilter;
    options.AutoReconnect = stdioOptions.AutoReconnect;
    options.HealthCheckInterval = stdioOptions.HealthCheckInterval;
});

// ==================== 服务注册 ====================

// 注册核心服务 IStdioToSseService
builder.Services.AddSingleton<IStdioToSseService, StdioToSseService>();

// 注册 MCP Server（用于 MCP 原生协议）
builder.Services.AddStdioToHttpMcpServer(builder.Configuration);

// 添加 Controllers 支持（用于管理端点）
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 添加 Swagger（可选，开发环境推荐）
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "MCP Proxy Management API",
            Version = "v1.0",
            Description = "Management endpoints for MCP Proxy HTTP/SSE Server"
        });

        // 启用 XML 注释
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });
}

// 健康检查
builder.Services.AddHealthChecks();

// ==================== 应用构建 ====================

WebApplication app = builder.Build();

// ==================== 中间件配置 ====================

// Swagger UI（开发环境）
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Proxy Management API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

// 映射 Controllers（管理 API）
app.MapControllers();

// 映射 MCP 协议端点（/mcp）- 这会触发初始化
app.MapStdioToHttpMcp();

// 健康检查端点
app.MapHealthChecks("/health");

// ==================== 启动应用 ====================

app.Logger.LogInformation(
    "MCP Proxy HTTP/SSE Server started with {ServerCount} backend server(s)",
    mcpServers.Count);

app.Logger.LogInformation("MCP endpoint: /mcp");
app.Logger.LogInformation("Health check: /health");
app.Logger.LogInformation("Server status: /api/servers");
app.Logger.LogInformation("Capabilities: /api/capabilities");

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("Swagger UI: / (root)");
}

await app.RunAsync();


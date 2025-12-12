// Copyright (c) IdeaTech. All rights reserved.

using McpProxy.Abstractions.Services;
using McpProxy.Core.Configuration;
using McpProxy.Core.Services;

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

builder.Services.Configure<HttpServerOptions>(
    builder.Configuration.GetSection("HttpServer"));

// ==================== 服务注册 ====================

// 核心服务
builder.Services.AddSingleton<IStdioToSseService, StdioToSseService>();

// ASP.NET Core 服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger 配置
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "MCP Proxy API",
        Version = "v1.0",
        Description = "Convert Stdio MCP servers to HTTP/SSE endpoints",
        Contact = new()
        {
            Name = "MCP Proxy",
            Url = new Uri("https://github.com/shuaihuadu/mcp-proxy")
        }
    });

    // 启用 XML 注释（如果存在）
    string xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.TagActionsBy(api => [api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default"]);
    options.DocInclusionPredicate((name, api) => true);
});

// CORS 配置
HttpServerOptions? httpOptions = builder.Configuration
    .GetSection("HttpServer")
    .Get<HttpServerOptions>();

if (httpOptions?.AllowedOrigins?.Count > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (httpOptions.AllowedOrigins.Contains("*"))
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(httpOptions.AllowedOrigins.ToArray())
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
        });
    });
}

// 健康检查
builder.Services.AddHealthChecks();

// 响应压缩
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ==================== 应用构建 ====================

WebApplication app = builder.Build();

// 初始化 MCP 服务连接
IStdioToSseService mcpService = app.Services.GetRequiredService<IStdioToSseService>();
await mcpService.InitializeAsync(app.Lifetime.ApplicationStopping);

// ==================== 中间件配置 ====================

// Swagger UI (开发环境)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Proxy API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
        options.DocumentTitle = "MCP Proxy API";
        options.DisplayRequestDuration();
    });
}

app.UseResponseCompression();

if (httpOptions?.AllowedOrigins?.Count > 0)
{
    app.UseCors();
}

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

// ==================== 启动应用 ====================

app.Logger.LogInformation("MCP Proxy Web API starting on {Urls} with {ServerCount} MCP server(s)", string.Join(", ", app.Urls), mcpServers.Count);

await app.RunAsync();


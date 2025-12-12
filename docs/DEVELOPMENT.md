# MCP Proxy 开发文档

> **面向对象**: 开发人员、维护人员  
> **文档版本**: v2.0  
> **最后更新**: 2025-12-09

---

## 📋 目录

- [1. 开发环境](#1-开发环境)
- [2. 项目结构](#2-项目结构)
- [3. 核心流程](#3-核心流程)
- [4. 扩展开发](#4-扩展开发)
- [5. 测试指南](#5-测试指南)
- [6. 调试技巧](#6-调试技巧)
- [7. 性能优化](#7-性能优化)
- [8. 常见问题](#8-常见问题)

---

## 1. 开发环境

### 1.1 环境要求

| 工具 | 版本要求 | 用途 |
|------|---------|------|
| **.NET SDK** | 10.0+ | 编译和运行 |
| **Visual Studio 2024** | 17.12+ | IDE (推荐) |
| **Visual Studio Code** | latest | 轻量级编辑器 |
| **Git** | 2.0+ | 版本控制 |
| **Docker Desktop** | latest | 容器化（可选） |
| **Node.js** | 18+ | 测试 MCP 服务器 |

### 1.2 安装步骤

#### 1. 克隆项目

```bash
git clone <repository-url>
cd mcp_proxy
```

#### 2. 恢复依赖

```bash
dotnet restore
```

#### 3. 构建项目

```bash
dotnet build
```

#### 4. 运行测试

```bash
dotnet test
```

### 1.3 IDE 配置

#### Visual Studio 2024

1. 打开 `mcp_proxy.sln`
2. 设置启动项目
   - **Web API**: `McpProxy.StdioToSse.WebApi`
   - **Host Service**: `McpProxy.SseToStdio.Host`
3. 配置调试配置

#### Visual Studio Code

安装推荐扩展:
- C# Dev Kit
- C# Extensions
- REST Client
- Docker

创建 `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch Web API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/McpProxy.StdioToSse.WebApi/bin/Debug/net10.0/McpProxy.StdioToSse.WebApi.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/McpProxy.StdioToSse.WebApi",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

---

## 2. 项目结构

### 2.1 目录结构

```
mcp_proxy/
├── src/
│   ├── McpProxy.Abstractions/          # 抽象层
│   │   ├── IMcpServerConfiguration.cs  # 配置接口
│   │   ├── IMcpServerDiscovery.cs      # 发现接口
│   │   ├── IMcpServerProvider.cs       # 提供者接口
│   │   ├── IMcpServerHealthCheck.cs    # 健康检查接口
│   │   └── Services/
│   │       ├── IStdioToSseService.cs   # Stdio→SSE 服务接口
│   │       ├── ISseToStdioService.cs   # SSE→Stdio 服务接口
│   │       └── IProxyService.cs        # 代理服务基类
│   │
│   ├── McpProxy.Core/                  # 核心业务层
│   │   ├── Configuration/              # 配置类
│   │   │   ├── McpServerConfig.cs      # MCP 服务器配置
│   │   │   ├── StdioServersOptions.cs  # Stdio 服务器选项
│   │   │   ├── SseClientOptions.cs     # SSE 客户端选项
│   │   │   ├── HttpServerOptions.cs    # HTTP 服务器选项
│   │   │   ├── OAuth2ClientCredentialsOptions.cs  # OAuth2 选项
│   │   │   └── ProxyServerOptions.cs   # 代理服务器选项
│   │   │
│   │   ├── Services/                   # 业务服务
│   │   │   ├── StdioToSseService.cs    # Stdio→SSE 实现
│   │   │   └── SseToStdioProxyService.cs  # SSE→Stdio 实现
│   │   │
│   │   └── Authentication/             # 认证
│   │       └── OAuth2ClientCredentialsHandler.cs  # OAuth2 处理器
│   │
│   ├── McpProxy.StdioToSse.WebApi/    # Web API 应用
│   │   ├── Program.cs                  # 应用入口
│   │   ├── appsettings.json           # 配置文件
│   │   └── README.md                   # 项目文档
│   │
│   ├── McpProxy.SseToStdio.Host/      # 后台服务应用
│   │   ├── Program.cs                  # 应用入口
│   │   ├── SseToStdioWorker.cs        # BackgroundService
│   │   ├── appsettings.json           # 配置文件
│   │   └── README.md                   # 项目文档
│   │
│   └── McpProxy.Cli/                   # CLI 工具（向后兼容）
│       ├── Program.cs
│       └── README.md
│
├── tests/
│   └── McpProxy.Tests/                 # 单元测试
│       └── Configuration/
│           └── ProxyServerOptionsTests.cs
│
└── docs/                               # 文档
    ├── ARCHITECTURE.md                 # 架构设计文档
    ├── DEVELOPMENT.md                  # 开发文档（本文件）
    ├── USER_GUIDE.md                   # 用户指南
    └── README.md                       # 项目说明
```

### 2.2 项目依赖关系

```
┌────────────────────────────────────────────────────────────┐
│                      依赖关系图                             │
└────────────────────────────────────────────────────────────┘

McpProxy.StdioToSse.WebApi ─┐
                             │
McpProxy.SseToStdio.Host ───┼──→ McpProxy.Core ──→ McpProxy.Abstractions
                             │           ↓
McpProxy.Cli ────────────────┘           ↓
                                   MCP SDK + .NET

规则:
✅ 应用层依赖业务层
✅ 业务层依赖抽象层
✅ 抽象层不依赖任何项目
✅ 所有层依赖 MCP SDK
```

### 2.3 命名空间约定

| 命名空间 | 用途 |
|---------|------|
| `McpProxy.Abstractions` | 核心接口和抽象 |
| `McpProxy.Abstractions.Services` | 服务接口 |
| `McpProxy.Abstractions.Models` | 数据模型 |
| `McpProxy.Core` | 核心业务逻辑 |
| `McpProxy.Core.Configuration` | 配置类 |
| `McpProxy.Core.Services` | 服务实现 |
| `McpProxy.Core.Authentication` | 认证相关 |

---

## 3. 核心流程

### 3.1 Stdio → SSE 流程

#### 初始化流程

```csharp
// 1. 读取配置
var stdioOptions = builder.Configuration
    .GetSection("StdioServers")
    .Get<StdioServersOptions>();

// 2. 注册服务
builder.Services.Configure<StdioServersOptions>(
    builder.Configuration.GetSection("StdioServers"));
builder.Services.AddSingleton<IStdioToSseService, StdioToSseService>();

// 3. 启动应用
var app = builder.Build();
var service = app.Services.GetRequiredService<IStdioToSseService>();

// 4. 初始化 MCP 连接
await service.InitializeAsync(cancellationToken);
```

#### 请求处理流程

```
HTTP Request
    ↓
API Endpoint (Program.cs)
    ↓
IStdioToSseService.ListToolsAsync(serverFilter)
    ↓
StdioToSseService
    ├─ 如果有 serverFilter
    │   └─ ListToolsFromServerAsync(serverName)
    │       └─ QueryServerSafelyAsync()
    │           └─ McpClient.ListToolsAsync()
    └─ 否则
        └─ ListAllToolsAsync()
            ├─ 并发查询所有服务器
            ├─ 添加命名空间前缀（可选）
            └─ 聚合结果
    ↓
JSON Response
```

#### 代码示例

```csharp
// StdioToSseService.cs
public async Task<ListToolsResult> ListToolsAsync(
    string? serverFilter = null,
    CancellationToken cancellationToken = default)
{
    this.EnsureInitialized();

    // 如果指定了服务器过滤器
    if (!string.IsNullOrEmpty(serverFilter) && this._options.AllowServerFilter)
    {
        this._logger.LogDebug("Listing tools for server: {ServerName}", serverFilter);
        return await this.ListToolsFromServerAsync(
            serverFilter,
            false,
            cancellationToken);
    }

    // 查询所有服务器并聚合
    this._logger.LogDebug("Listing tools from all servers");
    return await this.ListAllToolsAsync(
        this._options.UseNamespacePrefix,
        cancellationToken);
}

private async Task<ListToolsResult> ListAllToolsAsync(
    bool includePrefix,
    CancellationToken cancellationToken)
{
    // 并发查询所有服务器
    var tasks = this._servers.Values
        .Select(conn => this.ListToolsFromServerAsync(
            conn.Name,
            includePrefix,
            cancellationToken))
        .ToList();

    var results = await Task.WhenAll(tasks);

    // 聚合所有工具
    var allTools = results
        .SelectMany(r => r.Tools ?? Enumerable.Empty<Tool>())
        .ToList();

    return new ListToolsResult { Tools = allTools };
}
```

### 3.2 SSE → Stdio 流程

#### 连接流程

```csharp
// 1. 读取配置
var sseOptions = builder.Configuration
    .GetSection("SseClient")
    .Get<SseClientOptions>();

// 2. OAuth2 认证（如果配置）
if (sseOptions.OAuth2 != null)
{
    var handler = new OAuth2ClientCredentialsHandler(sseOptions.OAuth2);
    var httpClient = new HttpClient(handler);
}

// 3. 建立 SSE 连接
var transport = new SseClientTransport(sseOptions);
var mcpClient = await McpClient.CreateAsync(transport);

// 4. 转发 Stdio ↔ SSE
await service.RunAsync(cancellationToken);
```

#### 消息转发流程

```
Stdin (Console.ReadLine)
    ↓
SseToStdioProxyService
    ↓
解析 JSON-RPC 消息
    ↓
McpClient.SendMessageAsync()
    ↓
HTTP POST → SSE Endpoint
    ↓
接收 SSE 事件流
    ↓
解析响应消息
    ↓
Stdout (Console.WriteLine)
```

#### 代码示例

```csharp
// SseToStdioProxyService.cs
public async Task RunAsync(CancellationToken cancellationToken)
{
    try
    {
        // 建立连接
        await this.InitializeAsync(cancellationToken);

        // 启动消息循环
        while (!cancellationToken.IsCancellationRequested)
        {
            // 读取 Stdin
            var line = await Console.In.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;

            // 解析消息
            var message = JsonSerializer.Deserialize<JsonRpcMessage>(line);

            // 转发到 SSE
            var response = await this._mcpClient.SendAsync(message);

            // 写回 Stdout
            var json = JsonSerializer.Serialize(response);
            await Console.Out.WriteLineAsync(json);
        }
    }
    catch (OperationCanceledException)
    {
        // 优雅关闭
    }
}
```

### 3.3 多服务器聚合流程

#### 命名空间前缀

```csharp
// 工具名称格式: "servername:toolname"
private (string serverName, string itemName) ParseToolName(string fullName)
{
    if (string.IsNullOrEmpty(fullName))
    {
        throw new ArgumentException("Item name cannot be empty");
    }

    // 查找冒号分隔符
    int colonIndex = fullName.IndexOf(':', StringComparison.Ordinal);
    if (colonIndex > 0)
    {
        // 格式: "servername:itemname"
        string serverName = fullName.Substring(0, colonIndex);
        string itemName = fullName.Substring(colonIndex + 1);
        return (serverName, itemName);
    }

    // 如果只有一个服务器，直接使用
    if (this._servers.Count == 1)
    {
        string serverName = this._servers.Keys.First();
        return (serverName, fullName);
    }

    // 多服务器必须包含前缀
    throw new InvalidOperationException(
        $"Item name '{fullName}' must include server prefix " +
        "when multiple servers are configured");
}
```

#### 聚合示例

```json
// 配置多个服务器
{
  "StdioServers": {
    "Servers": [
      {
        "Name": "filesystem",
        "Command": "npx",
        "Arguments": ["-y", "@modelcontextprotocol/server-filesystem"]
      },
      {
        "Name": "github",
        "Command": "npx",
        "Arguments": ["-y", "@modelcontextprotocol/server-github"]
      }
    ],
    "UseNamespacePrefix": true
  }
}

// 聚合后的工具列表
{
  "tools": [
    { "name": "filesystem:read_file", ... },
    { "name": "filesystem:write_file", ... },
    { "name": "github:create_issue", ... },
    { "name": "github:list_repos", ... }
  ]
}

// 调用工具时指定服务器
POST /api/mcp/tools/call
{
  "name": "filesystem:read_file",
  "arguments": { "path": "/etc/hosts" }
}
```

---

## 4. 扩展开发

### 4.1 添加新的服务器发现策略

#### 1. 定义接口实现

```csharp
// MyCustomDiscovery.cs
using McpProxy.Abstractions;

namespace MyExtension;

public class DatabaseServerDiscovery : IMcpServerDiscovery
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<DatabaseServerDiscovery> _logger;

    public string Name => "Database";

    public bool SupportsHotReload => true;

    public DatabaseServerDiscovery(
        IDbContext dbContext,
        ILogger<DatabaseServerDiscovery> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IMcpServerConfiguration>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering servers from database");

        // 从数据库读取配置
        var entities = await _dbContext.McpServers
            .Where(s => s.Enabled)
            .ToListAsync(cancellationToken);

        // 转换为配置接口
        return entities.Select(e => new McpServerConfig
        {
            Id = e.Id.ToString(),
            Name = e.Name,
            Command = e.Command,
            Arguments = e.Arguments?.Split(',').ToList(),
            Environment = e.Environment,
            WorkingDirectory = e.WorkingDirectory,
            Enabled = e.Enabled
        }).ToList();
    }

    public async Task WatchAsync(
        Func<IReadOnlyList<IMcpServerConfiguration>, Task> callback,
        CancellationToken cancellationToken = default)
    {
        // 使用数据库变更通知（如 SQL Server Change Tracking）
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

            // 检查变更
            var servers = await DiscoverAsync(cancellationToken);
            await callback(servers);
        }
    }
}
```

#### 2. 注册服务

```csharp
// Program.cs
builder.Services.AddDbContext<IDbContext, MyDbContext>();
builder.Services.AddSingleton<IMcpServerDiscovery, DatabaseServerDiscovery>();
builder.Services.AddSingleton<IMcpServerProvider, CompositeServerProvider>();
```

#### 3. 使用示例

```csharp
public class CompositeServerProvider : IMcpServerProvider
{
    private readonly IEnumerable<IMcpServerDiscovery> _discoveries;

    public async Task<IReadOnlyList<IMcpServerConfiguration>> GetServersAsync(...)
    {
        var allServers = new List<IMcpServerConfiguration>();

        // 从所有发现策略收集
        foreach (var discovery in _discoveries)
        {
            var servers = await discovery.DiscoverAsync(cancellationToken);
            allServers.AddRange(servers);
        }

        // 去重
        return allServers
            .GroupBy(s => s.Name)
            .Select(g => g.First())
            .ToList();
    }
}
```

### 4.2 添加自定义中间件

#### 1. 创建中间件

```csharp
// RequestLoggingMiddleware.cs
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 记录请求
        _logger.LogInformation(
            "Request: {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        var startTime = DateTime.UtcNow;

        try
        {
            await _next(context);
        }
        finally
        {
            // 记录响应
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Response: {StatusCode} in {Duration}ms",
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        }
    }
}
```

#### 2. 注册中间件

```csharp
// Program.cs
app.UseMiddleware<RequestLoggingMiddleware>();
```

### 4.3 添加自定义健康检查

```csharp
// McpServerHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class McpServerHealthCheck : IHealthCheck
{
    private readonly IStdioToSseService _service;

    public McpServerHealthCheck(IStdioToSseService service)
    {
        _service = service;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查服务器状态
            var servers = await _service.GetServerStatusAsync();
            var connectedCount = servers.Count(s => s.IsConnected);

            if (connectedCount == 0)
            {
                return HealthCheckResult.Unhealthy(
                    "No MCP servers are connected");
            }

            if (connectedCount < servers.Count)
            {
                return HealthCheckResult.Degraded(
                    $"{connectedCount}/{servers.Count} servers connected");
            }

            return HealthCheckResult.Healthy(
                $"All {connectedCount} servers are connected");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Health check failed",
                ex);
        }
    }
}

// 注册
builder.Services.AddHealthChecks()
    .AddCheck<McpServerHealthCheck>("mcp_servers");
```

### 4.4 添加自定义认证

```csharp
// ApiKeyAuthenticationHandler.cs
using Microsoft.AspNetCore.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 检查 API Key
        if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues))
        {
            return AuthenticateResult.Fail("Missing API Key");
        }

        var apiKey = apiKeyValues.FirstOrDefault();
        if (apiKey != Options.ApiKey)
        {
            return AuthenticateResult.Fail("Invalid API Key");
        }

        // 创建 ClaimsPrincipal
        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

// 注册
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
```

---

## 5. 测试指南

### 5.1 单元测试

#### 测试配置类

```csharp
[TestClass]
public class McpServerConfigTests
{
    [TestMethod]
    public void McpServerConfig_ImplementsInterface()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Name = "test-server",
            Command = "npx",
            Arguments = new List<string> { "-y", "server" }
        };

        // Act
        IMcpServerConfiguration interfaceConfig = config;

        // Assert
        Assert.AreEqual("test-server", interfaceConfig.Name);
        Assert.AreEqual(2, interfaceConfig.Arguments.Count);
    }

    [TestMethod]
    public void McpServerConfig_ValidatesRequired()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Name = "",
            Command = "npx"
        };

        // Act
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(config, context, results, true);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(results.Any(r => r.MemberNames.Contains("Name")));
    }
}
```

#### 测试服务逻辑

```csharp
[TestClass]
public class StdioToSseServiceTests
{
    [TestMethod]
    public async Task ListToolsAsync_WithServerFilter_ReturnsFilteredTools()
    {
        // Arrange
        var mockOptions = new StdioServersOptions
        {
            Servers = new List<McpServerConfig>
            {
                new() { Name = "server1", Command = "npx" }
            }
        };
        var service = new StdioToSseService(
            Options.Create(mockOptions),
            Mock.Of<ILogger<StdioToSseService>>());

        await service.InitializeAsync();

        // Act
        var result = await service.ListToolsAsync("server1");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Tools);
    }
}
```

### 5.2 集成测试

```csharp
[TestClass]
public class WebApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.Test.json");
                });
            });

        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task ListTools_ReturnsTools()
    {
        // Act
        var response = await _client.PostAsync("/api/mcp/tools/list", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ListToolsResult>(json);
        Assert.IsNotNull(result);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

### 5.3 性能测试

```bash
# 使用 Apache Bench
ab -n 1000 -c 10 http://localhost:3000/api/mcp/tools/list

# 使用 wrk
wrk -t4 -c100 -d30s http://localhost:3000/api/mcp/tools/list

# 使用 k6
k6 run performance-test.js
```

```javascript
// performance-test.js
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  vus: 100,
  duration: '30s',
};

export default function () {
  const res = http.post('http://localhost:3000/api/mcp/tools/list');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
```

---

## 6. 调试技巧

### 6.1 日志调试

```csharp
// 设置日志级别
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 结构化日志
_logger.LogInformation(
    "Processing tool call: {ToolName} with {ArgCount} arguments",
    toolName,
    arguments?.Count ?? 0);

// 作用域日志
using (_logger.BeginScope("ServerName:{ServerName}", serverName))
{
    _logger.LogDebug("Connecting to server");
    // ...
}
```

### 6.2 断点调试

Visual Studio:
1. 设置断点: `F9`
2. 启动调试: `F5`
3. 单步执行: `F10`
4. 步入函数: `F11`
5. 查看变量: 鼠标悬停或监视窗口

### 6.3 HTTP 请求调试

```bash
# 使用 curl
curl -X POST http://localhost:3000/api/mcp/tools/list \
  -H "Content-Type: application/json" \
  -d '{}' \
  -v

# 使用 httpie
http POST http://localhost:3000/api/mcp/tools/list

# 使用 Postman 或 Insomnia
```

### 6.4 Stdio 调试

```bash
# 手动测试 Stdio MCP 服务器
echo '{"jsonrpc":"2.0","method":"initialize","params":{},"id":1}' | npx -y @modelcontextprotocol/server-everything

# 捕获 Stdio 流
tee debug.log | mcpproxy | tee -a debug.log
```

---

## 7. 性能优化

### 7.1 异步优化

```csharp
// ❌ 错误：同步阻塞
var result = service.ListToolsAsync().Result;

// ✅ 正确：异步等待
var result = await service.ListToolsAsync();

// ✅ 并发处理
var tasks = servers.Select(s => ProcessServerAsync(s));
var results = await Task.WhenAll(tasks);
```

### 7.2 缓存优化

```csharp
// 添加输出缓存
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("tools", builder =>
        builder.Expire(TimeSpan.FromMinutes(5)));
});

app.MapPost("/api/mcp/tools/list", ...)
    .CacheOutput("tools");
```

### 7.3 连接池优化

```csharp
// HTTP 客户端工厂
builder.Services.AddHttpClient<ISseToStdioService, SseToStdioProxyService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 50
        });
```

### 7.4 内存优化

```csharp
// 使用 ArrayPool
var buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    // 使用 buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// 使用 ValueTask
public ValueTask<int> GetCountAsync()
{
    if (_cache.TryGetValue("count", out int count))
    {
        return new ValueTask<int>(count);
    }
    
    return new ValueTask<int>(LoadCountAsync());
}
```

---

## 8. 常见问题

### 8.1 连接问题

**问题**: MCP 服务器连接失败

```
错误: Failed to connect to server 'xxx': Process exited with code 1
```

**解决**:
```csharp
// 1. 检查命令是否正确
Command = "npx",
Arguments = ["-y", "@modelcontextprotocol/server-everything"]

// 2. 检查工作目录
WorkingDirectory = "/path/to/project"

// 3. 检查环境变量
Environment = new Dictionary<string, string>
{
    { "NODE_ENV", "production" }
}

// 4. 增加超时时间
var timeout = TimeSpan.FromSeconds(60);
await task.WaitAsync(timeout);
```

### 8.2 序列化问题

**问题**: JSON 序列化失败

```
错误: JsonException: A possible object cycle was detected
```

**解决**:
```csharp
// 配置 JSON 选项
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
```

### 8.3 OAuth2 问题

**问题**: OAuth2 认证失败

```
错误: 401 Unauthorized
```

**解决**:
```csharp
// 1. 检查配置
OAuth2 = new OAuth2ClientCredentialsOptions
{
    ClientId = "your-client-id",          // 检查是否正确
    ClientSecret = "your-client-secret",  // 检查是否正确
    TokenUrl = "https://auth.example.com/token",  // 检查 URL
    Scope = "mcp.read mcp.write"          // 检查作用域
}

// 2. 测试 token 获取
var handler = new OAuth2ClientCredentialsHandler(oauth2Options);
var token = await handler.GetAccessTokenAsync();
Console.WriteLine($"Token: {token}");

// 3. 手动测试认证
curl -X POST https://auth.example.com/token \
  -d "grant_type=client_credentials" \
  -d "client_id=xxx" \
  -d "client_secret=xxx"
```

### 8.4 性能问题

**问题**: API 响应慢

**诊断**:
```csharp
// 添加性能监控
var stopwatch = Stopwatch.StartNew();
try
{
    var result = await service.ListToolsAsync();
    return Results.Ok(result);
}
finally
{
    stopwatch.Stop();
    _logger.LogInformation(
        "ListTools completed in {Duration}ms",
        stopwatch.ElapsedMilliseconds);
}
```

**优化**:
```csharp
// 1. 使用并发
var tasks = servers.Select(s => s.ListToolsAsync());
var results = await Task.WhenAll(tasks);

// 2. 添加超时
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await service.ListToolsAsync(ct: cts.Token);

// 3. 添加缓存
app.MapPost("/api/mcp/tools/list", ...)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
```

---

## 附录

### A. 常用命令

```bash
# 构建
dotnet build

# 运行测试
dotnet test

# 发布
dotnet publish -c Release

# 创建 Docker 镜像
docker build -t mcp-proxy:latest .

# 运行 Docker 容器
docker run -p 3000:3000 mcp-proxy:latest

# 查看日志
dotnet run --no-build 2>&1 | tee app.log
```

### B. 代码规范

- 使用 `async/await` 进行异步编程
- 使用 `CancellationToken` 支持取消
- 使用 `ILogger` 记录日志
- 使用 `IOptions` 注入配置
- 遵循 SOLID 原则
- 编写单元测试

### C. Git 工作流

```bash
# 创建特性分支
git checkout -b feature/my-feature

# 提交变更
git add .
git commit -m "Add: new feature"

# 推送到远程
git push origin feature/my-feature

# 创建 Pull Request
```

---

**文档维护者**: MCP Proxy Team  
**反馈渠道**: GitHub Issues  
**更新频率**: 每个版本发布

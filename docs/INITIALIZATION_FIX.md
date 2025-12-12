# 修复初始化问题说明

## 问题原因

之前的代码使用临时 `ServiceProvider` 初始化 `StdioToHttpProxyService`，但 `WebApplication.Build()` 时会创建新的服务实例，导致真正使用的实例未初始化。

### 旧代码问题：

```csharp
// 临时 ServiceProvider（这个实例会被初始化）
using var tempServiceProvider = builder.Services.BuildServiceProvider();
var proxyService = tempServiceProvider.GetRequiredService<StdioToHttpProxyService>();
await proxyService.InitializeAsync();

// 真正的应用（会创建新实例，未初始化）❌
WebApplication app = builder.Build();
```

---

## 解决方案

### 核心思路

在 `MapStdioToHttpMcp()` 方法中进行初始化，确保初始化的是真正被使用的实例。

### 新架构流程

```
1. builder.Services.AddStdioToHttpMcpServer()
   ↓
   注册 StdioToHttpProxyService (单例)
   注册 McpServerOptions (延迟创建)
   注册 AddMcpServer().WithHttpTransport()

2. app = builder.Build()
   ↓
   创建所有已注册的服务实例

3. app.MapStdioToHttpMcp()
   ↓
   获取 StdioToHttpProxyService 实例
   调用 InitializeAsync() ✅
   映射 MCP 端点
```

### 关键代码

#### `McpServerExtensions.cs`

```csharp
public static WebApplication MapStdioToHttpMcp(this WebApplication app)
{
    // 获取真正的服务实例
    var proxyService = app.Services.GetRequiredService<StdioToHttpProxyService>();
    
    // 初始化（仅一次）
    if (!_initialized)
    {
        await proxyService.InitializeAsync(app.Lifetime.ApplicationStopping);
        _initialized = true;
    }
    
    // 映射 MCP 端点
    app.MapMcp();
    return app;
}
```

#### `Program.cs`

```csharp
// 1. 注册服务
builder.Services.AddStdioToHttpMcpServer(builder.Configuration);

// 2. 构建应用
WebApplication app = builder.Build();

// 3. 映射端点（会触发初始化）✅
app.MapStdioToHttpMcp();
```

---

## 验证步骤

### 1. 启动应用

```bash
dotnet run --project src/McpProxy.StdioToSse.WebApi
```

### 2. 查看日志

应该看到：

```
info: McpProxy.Core.Services.StdioToHttpProxyService[0]
      Starting Stdio to HTTP proxy with 1 servers
info: McpProxy.Core.Services.StdioToHttpProxyService[0]
      Connecting to server 'everything': npx -y @modelcontextprotocol/server-everything
info: McpProxy.Core.Services.StdioToHttpProxyService[0]
      Connected to server 'everything': everything-server (1.0.0)
info: McpProxy.Core.Services.StdioToHttpProxyService[0]
      Connected to 1/1 servers
info: Program[0]
      MCP Proxy HTTP/SSE Server started with 1 backend server(s)
```

### 3. 测试端点

```bash
# 健康检查
curl http://localhost:5000/health

# 服务器状态
curl http://localhost:5000/api/servers

# MCP 协议测试
npx @modelcontextprotocol/inspector http://localhost:5000/mcp
```

---

## 技术细节

### 为什么使用 `SemaphoreSlim`？

确保多线程环境下只初始化一次：

```csharp
private static readonly SemaphoreSlim _initLock = new(1, 1);

await _initLock.WaitAsync();
try
{
    if (_initializedProxyService == null)
    {
        await proxyService.InitializeAsync();
        _initializedProxyService = proxyService;
    }
}
finally
{
    _initLock.Release();
}
```

### 为什么提供同步和异步版本？

- **`MapStdioToHttpMcpAsync()`**：异步版本，推荐使用
- **`MapStdioToHttpMcp()`**：同步包装，兼容 `WebApplication` 的同步配置风格

```csharp
// 异步版本（推荐）
await app.MapStdioToHttpMcpAsync();

// 同步版本（内部调用异步）
app.MapStdioToHttpMcp();
```

---

## 常见问题

### Q: 为什么不在构造函数中初始化？

A: 初始化是异步操作，不能在构造函数中调用 `async` 方法。

### Q: 为什么不使用 `IHostedService`？

A: `IHostedService` 会在应用启动后台运行，但我们需要在映射 MCP 端点之前完成初始化。

### Q: 如果忘记调用 `MapStdioToHttpMcp()` 会怎样？

A: 访问 `/mcp` 端点时，MCP Server 会尝试获取 `McpServerOptions`，此时会调用 `CreateAggregatedServerOptions()`，如果未初始化会抛出异常：

```
System.InvalidOperationException: Service not initialized. Call InitializeAsync first.
```

---

## 最佳实践

1. **始终在 `app.Build()` 之后调用 `MapStdioToHttpMcp()`**
2. **确保在映射其他端点之前完成 MCP 初始化**
3. **检查启动日志确认所有后端服务器已连接**

---

## 完整启动流程

```
用户运行 dotnet run
    ↓
Program.cs 加载配置
    ↓
AddStdioToHttpMcpServer() 注册服务
    ↓
app.Build() 创建服务实例
    ↓
MapStdioToHttpMcp() 初始化并映射端点 ✅
    ↓
app.RunAsync() 启动服务器
    ↓
用户访问 /mcp 端点 ✅
```

---

修复完成！现在 `StdioToHttpProxyService` 会在正确的时机被正确的实例初始化。

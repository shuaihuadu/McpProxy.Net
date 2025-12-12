# IMcpRuntime 移除迁移指南

## 📋 变更概述

在这次重构中，我们移除了 `IMcpRuntime` 接口及其实现类 `McpRuntime`，简化了架构层次。现在所有功能都直接通过 `IMcpProxyService` 提供。

---

## 🎯 为什么移除？

### 1. **职责重叠**
`IMcpRuntime` 的所有方法都只是简单委托给 `IMcpProxyService`，没有实际的业务逻辑。

### 2. **维护成本高**
需要同时维护两个接口和两套方法签名，增加了维护负担。

### 3. **架构简化**
移除后架构更清晰：

**之前**：
```
Application → IMcpRuntime → IMcpProxyService → MCP Servers
```

**现在**：
```
Application → IMcpProxyService → MCP Servers
```

---

## 🔄 迁移步骤

### 场景 1：直接使用 IMcpRuntime 的代码

#### Before（旧代码）

```csharp
public class MyService
{
    private readonly IMcpRuntime _runtime;
    
    public MyService(IMcpRuntime runtime)
    {
        _runtime = runtime;
    }
    
    public async Task DoSomethingAsync()
    {
        // 调用工具
        var tools = await _runtime.ListToolsHandler(null);
        var result = await _runtime.CallToolHandler(
            new CallToolRequestParams { Name = "tool1" });
        
        // 刷新
        await _runtime.RefreshAsync();
        
        // 获取状态
        var status = _runtime.GetStatus();
    }
}
```

#### After（新代码）

```csharp
public class MyService
{
    private readonly IMcpProxyService _proxyService;
    
    public MyService(IMcpProxyService proxyService)
    {
        _proxyService = proxyService;
    }
    
    public async Task DoSomethingAsync()
    {
        // 调用工具（方法名更直观）
        var tools = await _proxyService.ListToolsAsync();
        var result = await _proxyService.CallToolAsync(
            new CallToolRequestParams { Name = "tool1" });
        
        // 刷新（相同）
        await _proxyService.RefreshAsync();
        
        // 获取状态（相同）
        var status = _proxyService.GetStatus();
    }
}
```

**变化总结**：
- ✅ 依赖注入从 `IMcpRuntime` 改为 `IMcpProxyService`
- ✅ 方法名从 `*Handler` 改为 `*Async`（更简洁）
- ✅ 参数传递更直接（不需要包装在 `RequestContext` 中）
- ✅ `RefreshAsync()` 和 `GetStatus()` 方法保持不变

---

### 场景 2：DI 容器注册

#### Before（旧代码）

```csharp
// Program.cs 或 Startup.cs
builder.Services.AddSingleton<IMcpProxyService, StdioToHttpProxyService>();
builder.Services.AddSingleton<IMcpRuntime, McpRuntime>();
```

#### After（新代码）

```csharp
// Program.cs 或 Startup.cs
builder.Services.AddSingleton<IMcpProxyService, StdioToHttpProxyService>();
// 不再需要注册 IMcpRuntime
```

---

### 场景 3：WebServer 中的 McpServerOptions 配置

这部分已经在 `src/McpProxy.WebServer/Program.cs` 中更新。

#### Before（旧代码）

```csharp
builder.Services.AddOptions<McpServerOptions>().Configure<IMcpRuntime>((options, runtime) =>
{
    options.Handlers = new()
    {
        CallToolHandler = runtime.CallToolHandler,
        ListToolsHandler = runtime.ListToolsHandler,
        // ...
    };
});
```

#### After（新代码）

```csharp
builder.Services.AddOptions<McpServerOptions>().Configure<IMcpProxyService>((options, proxyService) =>
{
    options.Handlers = new()
    {
        CallToolHandler = async (request, cancellationToken) =>
        {
            if (request?.Params == null)
                throw new ArgumentNullException(nameof(request));
            return await proxyService.CallToolAsync(request.Params, cancellationToken);
        },
        
        ListToolsHandler = async (request, cancellationToken) =>
        {
            if (request?.Params != null)
                return await proxyService.ListToolsAsync(request.Params, cancellationToken);
            return await proxyService.ListToolsAsync(null, null, cancellationToken);
        },
        // ...
    };
});
```

---

## 📦 删除的文件

以下文件已被删除：

1. **接口定义**：
   - `src/McpProxy.Abstractions/Mcp/IMcpRuntime.cs`

2. **实现类**：
   - `src/McpProxy.Core/Mcp/McpRuntime.cs`

3. **测试文件**：
   - `tests/McpProxy.Core.UnitTests/McpRuntimeTests.cs`
   - `tests/McpProxy.Core.UnitTests/McpRuntimeSimplifiedTests.cs`

---

## ✅ 已更新的文件

1. **WebServer**：
   - `src/McpProxy.WebServer/Program.cs` - 更新为直接使用 `IMcpProxyService`

---

## 🎯 API 对照表

### Tools 操作

| 旧方法（IMcpRuntime） | 新方法（IMcpProxyService） |
|----------------------|---------------------------|
| `ListToolsHandler(RequestContext<ListToolsRequestParams>?)` | `ListToolsAsync(string? mcpServerName = null, string? cursor = null)` |
| `CallToolHandler(RequestContext<CallToolRequestParams>)` | `CallToolAsync(CallToolRequestParams)` |
| `CallToolHandler(CallToolRequestParams)` | `CallToolAsync(CallToolRequestParams)` |

### Prompts 操作

| 旧方法（IMcpRuntime） | 新方法（IMcpProxyService） |
|----------------------|---------------------------|
| `ListPromptsHandler(RequestContext<ListPromptsRequestParams>?)` | `ListPromptsAsync(string? mcpServerName = null, string? cursor = null)` |
| `GetPromptHandler(RequestContext<GetPromptRequestParams>)` | `GetPromptAsync(GetPromptRequestParams)` |

### Resources 操作

| 旧方法（IMcpRuntime） | 新方法（IMcpProxyService） |
|----------------------|---------------------------|
| `ListResourcesHandler(RequestContext<ListResourcesRequestParams>?)` | `ListResourcesAsync(string? mcpServerName = null, string? cursor = null)` |
| `ReadResourceHandler(RequestContext<ReadResourceRequestParams>)` | `ReadResourceAsync(ReadResourceRequestParams)` |
| `SubscribeResourceHandler(RequestContext<SubscribeRequestParams>)` | `SubscribeResourceAsync(SubscribeRequestParams)` |
| `UnsubscribeResourceHandler(RequestContext<UnsubscribeRequestParams>)` | `UnsubscribeResourceAsync(UnsubscribeRequestParams)` |

### 生命周期管理

| 旧方法（IMcpRuntime） | 新方法（IMcpProxyService） |
|----------------------|---------------------------|
| `RefreshAsync(CancellationToken)` | `RefreshAsync(CancellationToken)` ✅ 相同 |
| `GetStatus()` | `GetStatus()` ✅ 相同 |
| `ValidateAsync(CancellationToken)` | `ValidateAsync(CancellationToken)` ✅ 相同 |

---

## 💡 迁移建议

### 1. **批量替换**

使用 IDE 的重构功能：
1. 全局搜索 `IMcpRuntime`
2. 替换为 `IMcpProxyService`
3. 更新方法调用（`*Handler` → `*Async`）

### 2. **测试代码更新**

如果你的测试代码中使用了 `IMcpRuntime`：

```csharp
// Before
var mockRuntime = new Mock<IMcpRuntime>();
mockRuntime.Setup(r => r.ListToolsHandler(...)).ReturnsAsync(...);

// After
var mockProxyService = new Mock<IMcpProxyService>();
mockProxyService.Setup(p => p.ListToolsAsync(...)).ReturnsAsync(...);
```

### 3. **方法名映射**

使用 IDE 的查找替换功能：
- `ListToolsHandler` → `ListToolsAsync`
- `CallToolHandler` → `CallToolAsync`
- `ListPromptsHandler` → `ListPromptsAsync`
- `GetPromptHandler` → `GetPromptAsync`
- `ListResourcesHandler` → `ListResourcesAsync`
- `ReadResourceHandler` → `ReadResourceAsync`

---

## 🚀 优势

移除 `IMcpRuntime` 后的优势：

1. ✅ **代码更简洁**：减少一层抽象
2. ✅ **维护成本降低**：只需维护一个接口
3. ✅ **性能提升**：减少一次方法调用
4. ✅ **API 更直观**：方法名更符合 .NET 命名约定
5. ✅ **易于理解**：架构层次更清晰

---

## ❓ FAQ

### Q: RequestContext 还能使用吗？

A: 可以。`IMcpProxyService` 仍然提供接受 `RequestContext` 的重载方法，用于需要访问协议元数据的场景（如双向通信）。

### Q: 现有的 WebServer 代码会受影响吗？

A: 已经更新。`Program.cs` 中的配置已改为直接使用 `IMcpProxyService`，功能完全相同。

### Q: 如果我需要日志记录怎么办？

A: 可以使用以下方式：
- ASP.NET Core 中间件
- 装饰器模式包装 `IMcpProxyService`
- 直接在调用方添加日志

### Q: 性能会受影响吗？

A: 性能会**略有提升**，因为减少了一层方法调用。

---

## 📞 支持

如果在迁移过程中遇到问题，请参考：
- [IMcpProxyService API 文档](../api/IMcpProxyService.md)
- [RefreshAsync 使用指南](../features/RefreshAsync-Guide.md)
- 提交 Issue：https://github.com/shuaihuadu/McpProxy.Net/issues

---

**迁移完成后，你的代码将更简洁、更易维护！** 🎉

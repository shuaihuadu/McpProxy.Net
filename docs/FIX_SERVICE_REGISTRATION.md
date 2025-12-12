# 修复：IStdioToSseService 未注册错误

## ❌ 错误信息

```
System.InvalidOperationException: 'No service for type 'McpProxy.Abstractions.Services.IStdioToSseService' has been registered.'
```

---

## 🔍 问题原因

在合并 `StdioToHttpProxyService` 和 `StdioToSseService` 后，`Program.cs` 中**缺少 `IStdioToSseService` 的注册**。

### 错误的代码（之前）

```csharp
// ❌ 没有注册 IStdioToSseService
builder.Services.AddStdioToHttpMcpServer(builder.Configuration);
```

`AddStdioToHttpMcpServer()` 内部尝试获取 `IStdioToSseService`，但该服务从未被注册：

```csharp
public static IServiceCollection AddStdioToHttpMcpServer(...)
{
    services.AddSingleton(sp =>
    {
        // ❌ 这里会抛出异常，因为 IStdioToSseService 未注册
        var service = sp.GetRequiredService<IStdioToSseService>();
        return service.CreateAggregatedServerOptions();
    });
}
```

---

## ✅ 解决方案

在 `Program.cs` 中**显式注册 `IStdioToSseService`**：

```csharp
// ✅ 添加这一行
builder.Services.AddSingleton<IStdioToSseService, StdioToSseService>();

// 然后再注册 MCP Server
builder.Services.AddStdioToHttpMcpServer(builder.Configuration);
```

---

## 📝 完整的注册顺序

```csharp
// 1. 配置选项
builder.Services.Configure<StdioServersOptions>(options => { ... });

// 2. 注册核心服务（必须在前）✅
builder.Services.AddSingleton<IStdioToSseService, StdioToSseService>();

// 3. 注册 MCP Server（依赖 IStdioToSseService）
builder.Services.AddStdioToHttpMcpServer(builder.Configuration);

// 4. 其他服务
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
```

---

## 🎯 为什么需要显式注册？

### 依赖关系

```
Program.cs
    ↓ 注册
IStdioToSseService (接口)
    ↓ 实现
StdioToSseService (实现类)
    ↓ 被依赖
AddStdioToHttpMcpServer (扩展方法)
    ↓ 使用
McpServerOptions (通过 CreateAggregatedServerOptions)
```

如果不注册 `IStdioToSseService`，扩展方法 `AddStdioToHttpMcpServer` 内部调用 `GetRequiredService<IStdioToSseService>()` 时会抛出异常。

---

## 🔧 详细修复步骤

### 步骤 1：添加 using 引用

```csharp
using McpProxy.Abstractions.Services;
using McpProxy.Core.Services;
```

### 步骤 2：注册服务

```csharp
builder.Services.AddSingleton<IStdioToSseService, StdioToSseService>();
```

### 步骤 3：验证

运行应用，不应再出现错误：

```sh
dotnet run --project src/McpProxy.StdioToSse.WebApi
```

---

## ✅ 验证清单

- [x] 添加 `using McpProxy.Abstractions.Services;`
- [x] 添加 `using McpProxy.Core.Services;`
- [x] 注册 `IStdioToSseService` → `StdioToSseService`
- [x] 确保注册顺序正确（在 `AddStdioToHttpMcpServer` 之前）
- [x] 编译成功
- [x] 应用启动正常

---

## 📊 其他可能使用 `IStdioToSseService` 的地方

确保以下地方都能正常工作：

1. **Controllers**
   ```csharp
   public class ManagementController : ControllerBase
   {
       private readonly IStdioToSseService _service;
   }
   ```

2. **MCP 扩展方法**
   ```csharp
   var service = app.Services.GetRequiredService<IStdioToSseService>();
   await service.InitializeAsync();
   ```

3. **MCP Server Options**
   ```csharp
   var options = service.CreateAggregatedServerOptions();
   ```

---

## 🎉 问题解决！

现在应用可以正常启动，所有端点都可以访问：

- ✅ `/mcp` - MCP 原生协议
- ✅ `/api/servers` - 服务器状态
- ✅ `/api/capabilities` - 聚合能力
- ✅ `/health` - 健康检查

---

## 💡 最佳实践

### 推荐的服务注册模式

```csharp
// 1. 配置
builder.Services.Configure<SomeOptions>(...);

// 2. 基础服务（无依赖）
builder.Services.AddSingleton<IBaseService, BaseService>();

// 3. 依赖服务（依赖基础服务）
builder.Services.AddDependent...();

// 4. 框架服务
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
```

### 避免循环依赖

确保依赖关系是单向的：

```
Config → Service → Extension → Application
```

---

修复完成！现在服务可以正常注册和使用。

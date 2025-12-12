# MCP Proxy 架构设计文档

> **面向对象**: 架构师、技术负责人  
> **文档版本**: v2.0  
> **最后更新**: 2025-12-09

---

## 📋 目录

- [1. 系统概述](#1-系统概述)
- [2. 架构设计](#2-架构设计)
- [3. 分层架构](#3-分层架构)
- [4. 核心模块](#4-核心模块)
- [5. 设计模式](#5-设计模式)
- [6. 技术选型](#6-技术选型)
- [7. 扩展性设计](#7-扩展性设计)
- [8. 部署架构](#8-部署架构)

---

## 1. 系统概述

### 1.1 系统定位

**MCP Proxy** 是一个基于 .NET 10 的高性能协议代理服务，实现 Model Context Protocol (MCP) 在不同传输方式（Stdio、HTTP/SSE）之间的转换和聚合。

### 1.2 核心价值

| 价值维度 | 说明 |
|---------|------|
| **协议转换** | 将 Stdio MCP 服务器暴露为 HTTP/SSE API |
| **服务聚合** | 多个 MCP 服务器统一聚合为单一端点 |
| **远程代理** | 将远程 SSE MCP 服务器转换为本地 Stdio 接口 |
| **生产就绪** | 健康检查、优雅关闭、自动重连、日志监控 |

### 1.3 应用场景

```
场景 1: Web 应用集成
┌─────────────┐      HTTP/SSE     ┌──────────────┐      Stdio      ┌─────────────┐
│  Web 前端    │ ←──────────────→ │  MCP Proxy   │ ←────────────→ │ 本地 MCP 服务│
│ (React/Vue) │                   │   Web API    │                 │   (Node.js)  │
└─────────────┘                   └──────────────┘                 └─────────────┘

场景 2: 远程服务代理
┌─────────────┐      Stdio        ┌──────────────┐      HTTP/SSE   ┌─────────────┐
│   AI 客户端  │ ←──────────────→ │  MCP Proxy   │ ←────────────→ │  远程 MCP    │
│  (Claude)   │                   │     Host     │                 │   服务 API   │
└─────────────┘                   └──────────────┘                 └─────────────┘

场景 3: 服务聚合
┌─────────────┐                   ┌──────────────┐                 ┌─────────────┐
│   AI 应用    │                   │  MCP Proxy   │ ←────────────→ │ MCP Server 1│
│             │ ←──────────────→ │   Aggregator │                 ├─────────────┤
│             │      单一端点      │              │ ←────────────→ │ MCP Server 2│
│             │                   │              │                 ├─────────────┤
│             │                   │              │ ←────────────→ │ MCP Server 3│
└─────────────┘                   └──────────────┘                 └─────────────┘
```

---

## 2. 架构设计

### 2.1 整体架构

采用**分层架构 + 模块化设计**，遵循 Clean Architecture 原则：

```
┌─────────────────────────────────────────────────────────────────────┐
│                          表示层 (Presentation)                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────────────┐       ┌──────────────────────────┐  │
│  │  McpProxy.StdioToSse     │       │  McpProxy.SseToStdio     │  │
│  │      .WebApi             │       │       .Host              │  │
│  ├──────────────────────────┤       ├──────────────────────────┤  │
│  │ • ASP.NET Core           │       │ • Generic Host           │  │
│  │ • Swagger/OpenAPI        │       │ • BackgroundService      │  │
│  │ • RESTful Endpoints      │       │ • System Service         │  │
│  │ • Health Checks          │       │ • Graceful Shutdown      │  │
│  └──────────────────────────┘       └──────────────────────────┘  │
│                                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                         应用层 (Application)                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                     McpProxy.Cli                             │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │ • Command Line Interface                                     │  │
│  │ • Configuration Management                                   │  │
│  │ • Backward Compatibility                                     │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                         业务层 (Domain)                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                     McpProxy.Core                            │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │  Services:                                                   │  │
│  │  • IStdioToSseService    - Stdio → SSE 转换                 │  │
│  │  • ISseToStdioService    - SSE → Stdio 转换                 │  │
│  │  • StdioToSseService     - 核心业务实现                      │  │
│  │  • SseToStdioProxyService - 代理服务实现                     │  │
│  │                                                               │  │
│  │  Configuration:                                               │  │
│  │  • ProxyServerOptions    - 代理服务器配置                     │  │
│  │  • StdioServersOptions   - Stdio 服务器配置                  │  │
│  │  • SseClientOptions      - SSE 客户端配置                     │  │
│  │  • HttpServerOptions     - HTTP 服务器配置                    │  │
│  │  • OAuth2Options         - OAuth2 认证配置                    │  │
│  │                                                               │  │
│  │  Authentication:                                              │  │
│  │  • OAuth2ClientCredentialsHandler - OAuth2 客户端凭据流      │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                        抽象层 (Abstractions)                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                  McpProxy.Abstractions                       │  │
│  ├──────────────────────────────────────────────────────────────┤  │
│  │  Interfaces:                                                 │  │
│  │  • IMcpServerConfiguration  - 服务器配置接口                 │  │
│  │  • IMcpServerDiscovery      - 服务器发现接口                 │  │
│  │  • IMcpServerProvider       - 服务器提供者接口               │  │
│  │  • IMcpServerHealthCheck    - 健康检查接口                   │  │
│  │  • IStdioToSseService       - Stdio→SSE 服务接口            │  │
│  │  • ISseToStdioService       - SSE→Stdio 服务接口            │  │
│  │  • IProxyService            - 代理服务基础接口               │  │
│  │                                                               │  │
│  │  Models:                                                      │  │
│  │  • ServerStatusInfo         - 服务器状态信息                 │  │
│  │  • McpServerHealthResult    - 健康检查结果                   │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                        基础设施层 (Infrastructure)                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  • Model Context Protocol SDK                                       │
│  • ASP.NET Core Framework                                           │
│  • Generic Host                                                      │
│  • System.Text.Json                                                 │
│  • Microsoft.Extensions.*                                           │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 依赖关系

```
McpProxy.StdioToSse.WebApi ─┐
                             ├──→ McpProxy.Core ──→ McpProxy.Abstractions
McpProxy.SseToStdio.Host ───┤              ↓
                             │              ↓
McpProxy.Cli ────────────────┘       MCP SDK + .NET
```

**依赖规则**：
- ✅ 外层依赖内层
- ✅ 内层不依赖外层
- ✅ 抽象层不依赖具体实现
- ✅ 业务层通过接口依赖基础设施

---

## 3. 分层架构

### 3.1 表示层 (Presentation Layer)

#### 职责
- HTTP 请求/响应处理
- API 端点暴露
- Swagger 文档生成
- 健康检查端点
- CORS 策略
- 请求验证

#### 核心组件

**McpProxy.StdioToSse.WebApi**
```csharp
// ASP.NET Core Minimal API
app.MapPost("/api/mcp/tools/list", async (
    [FromQuery] string? server,
    IStdioToSseService service,
    CancellationToken ct) =>
{
    var result = await service.ListToolsAsync(server, ct);
    return Results.Ok(result);
});
```

**McpProxy.SseToStdio.Host**
```csharp
// BackgroundService
public class SseToStdioWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _proxyService.RunAsync(stoppingToken);
    }
}
```

### 3.2 业务层 (Domain Layer)

#### 职责
- MCP 协议转换逻辑
- 多服务器连接管理
- 工具/提示/资源聚合
- 命名空间前缀处理
- 错误处理和重试

#### 核心服务

**IStdioToSseService**
```csharp
public interface IStdioToSseService
{
    // 生命周期管理
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    // MCP 协议操作
    Task<ListToolsResult> ListToolsAsync(string? serverFilter, CancellationToken ct);
    Task<CallToolResult> CallToolAsync(string toolName, object? args, CancellationToken ct);
    Task<ListPromptsResult> ListPromptsAsync(string? serverFilter, CancellationToken ct);
    Task<GetPromptResult> GetPromptAsync(string promptName, object? args, CancellationToken ct);
    Task<ListResourcesResult> ListResourcesAsync(string? serverFilter, CancellationToken ct);
    Task<ReadResourceResult> ReadResourceAsync(string resourceUri, CancellationToken ct);
    
    // 监控和管理
    Task<IReadOnlyList<ServerStatusInfo>> GetServerStatusAsync();
    ServerCapabilities GetAggregatedCapabilities();
}
```

#### 核心算法

**多服务器聚合**
```
输入: List<McpServerConfig> servers
     bool useNamespacePrefix
     
处理:
1. 并发连接所有启用的服务器
2. 为每个服务器创建 MCP 客户端连接
3. 查询所有服务器的 tools/prompts/resources
4. 如果 useNamespacePrefix = true:
   - 为每个项目添加 "servername:" 前缀
5. 聚合所有结果到单一列表
6. 去重（按名称）

输出: 聚合后的工具/提示/资源列表
```

**命名空间路由**
```
输入: toolName = "server1:tool_name"
     
解析:
1. 查找冒号分隔符位置
2. 提取服务器名称: "server1"
3. 提取实际工具名: "tool_name"
4. 路由到目标服务器执行

输出: 工具执行结果
```

### 3.3 抽象层 (Abstractions Layer)

#### 职责
- 定义核心接口契约
- 定义数据模型
- 提供扩展点
- 保证类型安全

#### 核心接口

**配置发现模式**
```
IMcpServerConfiguration  ← 配置实体
       ↑
       │ 产出
IMcpServerDiscovery ────→ IMcpServerProvider ─→ 应用层
   (发现策略)                 (统一提供者)
```

**扩展点设计**
```csharp
// 自定义发现策略
public class DatabaseServerDiscovery : IMcpServerDiscovery
{
    public async Task<IReadOnlyList<IMcpServerConfiguration>> DiscoverAsync(...)
    {
        // 从数据库读取配置
        var configs = await _dbContext.Servers.ToListAsync();
        return configs;
    }
}

// 注册到 DI 容器
services.AddSingleton<IMcpServerDiscovery, DatabaseServerDiscovery>();
```

---

## 4. 核心模块

### 4.1 协议转换模块

#### Stdio → HTTP/SSE

```
┌─────────────────────────────────────────────────────────────────┐
│                    Stdio to HTTP/SSE 转换流程                    │
└─────────────────────────────────────────────────────────────────┘

1. 初始化阶段
   ┌──────────────────────┐
   │ 读取配置              │
   │ - StdioServersOptions │
   │ - HttpServerOptions   │
   └───────────┬───────────┘
               ↓
   ┌──────────────────────┐
   │ 启动 MCP 客户端       │
   │ - 创建 Stdio 传输     │
   │ - 建立 MCP 连接       │
   │ - 交换能力信息        │
   └───────────┬───────────┘
               ↓
   ┌──────────────────────┐
   │ 启动 HTTP 服务器      │
   │ - 注册 API 端点       │
   │ - 配置 Swagger        │
   │ - 启用健康检查        │
   └───────────┬───────────┘

2. 请求处理阶段
   HTTP Request ──→ API Controller ──→ StdioToSseService
                                            ↓
                                   解析服务器前缀
                                            ↓
                                   路由到目标 MCP Client
                                            ↓
                                   发送 Stdio 消息
                                            ↓
                                   等待响应
                                            ↓
                                   聚合结果
                                            ↓
                                   返回 JSON
                                            ↓
                    HTTP Response ←── API Controller
```

#### SSE → Stdio

```
┌─────────────────────────────────────────────────────────────────┐
│                    SSE to Stdio 转换流程                         │
└─────────────────────────────────────────────────────────────────┘

1. 连接阶段
   ┌──────────────────────┐
   │ 读取配置              │
   │ - SseClientOptions    │
   │ - OAuth2Options       │
   └───────────┬───────────┘
               ↓
   ┌──────────────────────┐
   │ OAuth2 认证（可选）   │
   │ - 获取 access_token   │
   │ - 刷新 token          │
   └───────────┬───────────┘
               ↓
   ┌──────────────────────┐
   │ 建立 SSE 连接         │
   │ - 连接远程服务器      │
   │ - 处理 SSE 事件流     │
   └───────────┬───────────┘

2. 消息转发阶段
   Stdin ──→ SseToStdioService ──→ HTTP POST (SSE Endpoint)
                                            ↓
                                   接收 SSE 事件流
                                            ↓
                                   解析 JSON 消息
                                            ↓
   Stdout ←── SseToStdioService ←── 转换为 MCP 消息
```

### 4.2 配置管理模块

#### Options 模式

```csharp
// 配置绑定
builder.Services.Configure<StdioServersOptions>(
    builder.Configuration.GetSection("StdioServers"));

// 注入使用
public class MyService
{
    private readonly StdioServersOptions _options;
    
    public MyService(IOptions<StdioServersOptions> options)
    {
        _options = options.Value;
    }
}
```

#### 配置热重载

```csharp
// 支持配置文件变化时自动重载
builder.Configuration.AddJsonFile(
    "appsettings.json",
    optional: false,
    reloadOnChange: true);

// 使用 IOptionsMonitor 监听变化
public class MyService
{
    public MyService(IOptionsMonitor<StdioServersOptions> optionsMonitor)
    {
        optionsMonitor.OnChange(options =>
        {
            // 配置变化时重新初始化
            ReloadServers(options.Servers);
        });
    }
}
```

### 4.3 认证模块

#### OAuth2 客户端凭据流

```
┌─────────────────────────────────────────────────────────────────┐
│                    OAuth2 认证流程                               │
└─────────────────────────────────────────────────────────────────┘

1. 获取 Token
   ┌──────────────────────┐
   │ 准备认证请求          │
   │ - client_id           │
   │ - client_secret       │
   │ - grant_type          │
   │ - scope               │
   └───────────┬───────────┘
               ↓
   ┌──────────────────────┐
   │ POST /token          │
   │ → Token Endpoint     │
   └───────────┬───────────┘
               ↓
   ┌──────────────────────┐
   │ 解析响应              │
   │ - access_token        │
   │ - expires_in          │
   │ - token_type          │
   └───────────┬───────────┘

2. 使用 Token
   每个 HTTP 请求添加:
   Authorization: Bearer {access_token}

3. Token 刷新
   检查 expiry_time < now + 60s
   → 自动刷新 token
```

---

## 5. 设计模式

### 5.1 策略模式 (Strategy Pattern)

**用途**: 服务器发现策略

```csharp
// 策略接口
public interface IMcpServerDiscovery
{
    Task<IReadOnlyList<IMcpServerConfiguration>> DiscoverAsync(...);
}

// 具体策略
public class ConfigFileDiscovery : IMcpServerDiscovery { }
public class DatabaseDiscovery : IMcpServerDiscovery { }
public class ConsulDiscovery : IMcpServerDiscovery { }

// 运行时选择策略
services.AddSingleton<IMcpServerDiscovery, ConfigFileDiscovery>();
```

### 5.2 适配器模式 (Adapter Pattern)

**用途**: 配置类实现接口

```csharp
// 目标接口
public interface IMcpServerConfiguration
{
    string Name { get; }
    IReadOnlyList<string> Arguments { get; }
}

// 适配器
public class McpServerConfig : IMcpServerConfiguration
{
    // 可变配置
    public string? Name { get; set; }
    public List<string>? Arguments { get; set; }
    
    // 接口适配（只读）
    string IMcpServerConfiguration.Name => Name ?? Id;
    IReadOnlyList<string> IMcpServerConfiguration.Arguments => 
        Arguments?.AsReadOnly() ?? Array.Empty<string>();
}
```

### 5.3 代理模式 (Proxy Pattern)

**用途**: 协议转换

```csharp
// 真实对象 (MCP Server)
Stdio MCP Server

// 代理对象 (MCP Proxy)
public class StdioToSseService : IStdioToSseService
{
    private readonly McpClient _mcpClient;
    
    public async Task<ListToolsResult> ListToolsAsync(...)
    {
        // 转发到真实 MCP 服务器
        return await _mcpClient.ListToolsAsync(...);
    }
}
```

### 5.4 组合模式 (Composite Pattern)

**用途**: 多服务器聚合

```csharp
// 组合服务
public class CompositeServerProvider : IMcpServerProvider
{
    private readonly List<IMcpServerDiscovery> _discoveries;
    
    public async Task<IReadOnlyList<IMcpServerConfiguration>> GetServersAsync(...)
    {
        var allServers = new List<IMcpServerConfiguration>();
        
        // 组合多个发现策略
        foreach (var discovery in _discoveries)
        {
            var servers = await discovery.DiscoverAsync(...);
            allServers.AddRange(servers);
        }
        
        return allServers;
    }
}
```

### 5.5 观察者模式 (Observer Pattern)

**用途**: 配置变化通知

```csharp
public interface IMcpServerProvider
{
    event EventHandler<McpServerChangedEventArgs>? ServerChanged;
}

public class MyProvider : IMcpServerProvider
{
    public event EventHandler<McpServerChangedEventArgs>? ServerChanged;
    
    private void OnConfigChanged(IMcpServerConfiguration server)
    {
        ServerChanged?.Invoke(this, new McpServerChangedEventArgs(
            McpServerChangeType.Updated,
            server
        ));
    }
}
```

---

## 6. 技术选型

### 6.1 技术栈

| 层次 | 技术 | 版本 | 用途 |
|------|------|------|------|
| **运行时** | .NET | 10.0 | 应用运行时 |
| **Web 框架** | ASP.NET Core | 10.0 | Web API |
| **主机** | Generic Host | 10.0 | 后台服务 |
| **依赖注入** | Microsoft.Extensions.DependencyInjection | 10.0 | DI 容器 |
| **配置** | Microsoft.Extensions.Configuration | 10.0 | 配置管理 |
| **日志** | Microsoft.Extensions.Logging | 10.0 | 日志框架 |
| **序列化** | System.Text.Json | 10.0 | JSON 处理 |
| **HTTP 客户端** | HttpClient | 10.0 | HTTP 请求 |
| **MCP 协议** | ModelContextProtocol.NET | latest | MCP SDK |
| **API 文档** | Swashbuckle.AspNetCore | 7.2.0 | Swagger |
| **测试** | MSTest | 3.8.0 | 单元测试 |

### 6.2 技术决策

#### 为什么选择 .NET 10？

| 优势 | 说明 |
|------|------|
| **性能** | 原生 AOT、更快的 JSON 序列化 |
| **跨平台** | Windows、Linux、macOS |
| **现代语法** | C# 13 新特性 |
| **长期支持** | LTS 版本 |

#### 为什么选择 ASP.NET Core？

| 优势 | 说明 |
|------|------|
| **Kestrel** | 高性能 Web 服务器 |
| **Minimal API** | 简洁的 API 定义 |
| **中间件** | 灵活的请求管道 |
| **Swagger** | 自动 API 文档 |

#### 为什么选择 Generic Host？

| 优势 | 说明 |
|------|------|
| **统一** | Web 和非 Web 应用统一编程模型 |
| **DI** | 内置依赖注入 |
| **生命周期** | 优雅启动/关闭 |
| **后台任务** | BackgroundService 支持 |

---

## 7. 扩展性设计

### 7.1 水平扩展

```
┌─────────────────────────────────────────────────────────────────┐
│                       负载均衡架构                                │
└─────────────────────────────────────────────────────────────────┘

                    ┌──────────────┐
                    │ Load Balancer │
                    │  (Nginx/ALB)  │
                    └────────┬──────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
    ┌────▼────┐         ┌────▼────┐        ┌────▼────┐
    │ Proxy 1 │         │ Proxy 2 │        │ Proxy 3 │
    │ (Pod/VM)│         │ (Pod/VM)│        │ (Pod/VM)│
    └─────────┘         └─────────┘        └─────────┘
         │                   │                   │
         └───────────────────┼───────────────────┘
                             │
                    ┌────────▼──────┐
                    │  MCP Servers  │
                    │  (Shared Pool)│
                    └───────────────┘
```

**配置示例**:
```yaml
# Kubernetes Deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-proxy
spec:
  replicas: 3  # 水平扩展到 3 个副本
  selector:
    matchLabels:
      app: mcp-proxy
  template:
    spec:
      containers:
      - name: mcp-proxy
        image: mcp-proxy:latest
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

### 7.2 垂直扩展

```csharp
// 配置 Kestrel 限制
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});

// 配置线程池
ThreadPool.SetMinThreads(200, 200);
```

### 7.3 插件化扩展

```csharp
// 定义插件接口
public interface IMcpProxyPlugin
{
    string Name { get; }
    Task OnRequestAsync(HttpContext context);
    Task OnResponseAsync(HttpContext context);
}

// 实现插件
public class LoggingPlugin : IMcpProxyPlugin
{
    public string Name => "Logging";
    
    public async Task OnRequestAsync(HttpContext context)
    {
        // 请求日志
    }
}

// 注册插件
builder.Services.AddSingleton<IMcpProxyPlugin, LoggingPlugin>();
```

---

## 8. 部署架构

### 8.1 容器化部署

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 3000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["McpProxy.StdioToSse.WebApi/", "McpProxy.StdioToSse.WebApi/"]
COPY ["McpProxy.Core/", "McpProxy.Core/"]
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpProxy.StdioToSse.WebApi.dll"]
```

### 8.2 Kubernetes 部署

```yaml
apiVersion: v1
kind: Service
metadata:
  name: mcp-proxy-service
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 3000
  selector:
    app: mcp-proxy
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-proxy-deployment
spec:
  replicas: 3
  selector:
    matchLabels:
      app: mcp-proxy
  template:
    metadata:
      labels:
        app: mcp-proxy
    spec:
      containers:
      - name: mcp-proxy
        image: mcp-proxy:latest
        ports:
        - containerPort: 3000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 3000
          initialDelaySeconds: 5
          periodSeconds: 5
```

### 8.3 系统服务部署

**Windows Service**:
```bash
sc.exe create McpProxyService binPath="C:\path\to\McpProxy.SseToStdio.Host.exe"
sc.exe start McpProxyService
```

**Linux systemd**:
```ini
# /etc/systemd/system/mcp-proxy.service
[Unit]
Description=MCP Proxy Service
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/mcp-proxy
ExecStart=/opt/mcp-proxy/McpProxy.SseToStdio.Host
Restart=always

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable mcp-proxy
sudo systemctl start mcp-proxy
```

---

## 附录

### A. 术语表

| 术语 | 说明 |
|------|------|
| **MCP** | Model Context Protocol，AI 模型上下文协议 |
| **Stdio** | Standard Input/Output，标准输入输出 |
| **SSE** | Server-Sent Events，服务器推送事件 |
| **OAuth2** | Open Authorization 2.0，开放授权协议 |
| **DI** | Dependency Injection，依赖注入 |
| **AOT** | Ahead-of-Time，预编译 |

### B. 参考资料

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [.NET Generic Host](https://docs.microsoft.com/dotnet/core/extensions/generic-host)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**文档维护者**: MCP Proxy Team  
**反馈渠道**: GitHub Issues  
**更新频率**: 每个主版本发布

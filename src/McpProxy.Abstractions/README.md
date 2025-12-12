# McpProxy.Abstractions

MCP 服务器发现和配置管理的抽象层。

## 📋 概述

这个类库定义了 MCP 服务器发现、配置和管理的核心抽象接口，支持从多种来源（配置文件、数据库、服务发现等）动态加载和管理 MCP 服务器。

## 🎯 设计目标

### 1. **职责分离**
- **发现（Discovery）** - 从不同来源发现服务器配置
- **提供（Provider）** - 统一的服务器配置提供接口
- **健康检查（Health Check）** - 服务器健康状态监控

### 2. **扩展性**
- 支持多种配置源：配置文件、数据库、远程 API、服务发现
- 支持配置热重载
- 支持配置变化通知
- 支持自定义发现策略

### 3. **可测试性**
- 所有核心功能都是接口定义
- 便于单元测试和模拟
- 降低业务逻辑和配置管理的耦合

## 🏗️ 架构设计

### 核心接口关系

```
┌─────────────────────────────────────────────┐
│   IMcpServerConfiguration (配置实体)        │
├─────────────────────────────────────────────┤
│ - Id, Name, Command, Arguments             │
│ - Environment, WorkingDirectory            │
│ - Enabled, Tags, Metadata                  │
└─────────────────────────────────────────────┘
                    ↑
                    │ 产出
        ┌───────────┴───────────┐
        │                       │
┌───────────────┐       ┌───────────────────┐
│ IMcpServer    │       │ IMcpServer        │
│ Discovery     │──────>│ Provider          │
│ (发现策略)     │       │ (提供者)           │
├───────────────┤       ├───────────────────┤
│ - DiscoverAsync│       │ - GetServersAsync │
│ - WatchAsync   │       │ - GetServerByName │
└───────────────┘       │ - ServerChanged   │
        │               └───────────────────┘
        │                       ↑
        │                       │
┌───────────────┐               │
│ IMcpServer    │               │
│ HealthCheck   │───────────────┘
│ (健康检查)    │
└───────────────┘
```

### 实现示例

```
McpProxy.Abstractions (接口定义)
        ↓
McpProxy.Core (核心实现)
├── ConfigurationServerDiscovery     ← 从配置文件发现
├── DatabaseServerDiscovery          ← 从数据库发现
├── ConsulServerDiscovery            ← 从 Consul 服务发现
├── ConfigurationServerProvider      ← 配置文件提供者
├── CompositeServerProvider          ← 组合多个发现策略
└── CachedServerProvider             ← 缓存装饰器
        ↓
应用层 (使用)
├── McpProxy.Cli
├── McpProxy.StdioToSse.WebApi
└── McpProxy.SseToStdio.Host
```

## 📚 核心接口

### 1. `IMcpServerConfiguration`

表示单个 MCP 服务器的配置信息。

```csharp
public interface IMcpServerConfiguration
{
    string Id { get; }                              // 唯一标识符
    string Name { get; }                            // 服务器名称
    string Command { get; }                         // 启动命令
    IReadOnlyList<string> Arguments { get; }        // 命令参数
    IReadOnlyDictionary<string, string> Environment { get; }  // 环境变量
    string? WorkingDirectory { get; }               // 工作目录
    bool Enabled { get; }                           // 是否启用
    IReadOnlyList<string> Tags { get; }            // 标签
    IReadOnlyDictionary<string, object> Metadata { get; }     // 元数据
}
```

### 2. `IMcpServerDiscovery`

服务器发现策略接口，负责从特定来源发现配置。

```csharp
public interface IMcpServerDiscovery
{
    string Name { get; }                            // 策略名称
    
    // 发现服务器
    Task<IReadOnlyList<IMcpServerConfiguration>> DiscoverAsync(
        CancellationToken cancellationToken = default);
    
    // 是否支持热重载
    bool SupportsHotReload { get; }
    
    // 监听配置变化
    Task WatchAsync(
        Func<IReadOnlyList<IMcpServerConfiguration>, Task> callback,
        CancellationToken cancellationToken = default);
}
```

### 3. `IMcpServerProvider`

统一的服务器配置提供者接口。

```csharp
public interface IMcpServerProvider
{
    // 获取所有服务器
    Task<IReadOnlyList<IMcpServerConfiguration>> GetServersAsync(
        CancellationToken cancellationToken = default);
    
    // 按名称获取
    Task<IMcpServerConfiguration?> GetServerByNameAsync(
        string name,
        CancellationToken cancellationToken = default);
    
    // 按 ID 获取
    Task<IMcpServerConfiguration?> GetServerByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
    
    // 按标签过滤
    Task<IReadOnlyList<IMcpServerConfiguration>> GetServersByTagsAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default);
    
    // 配置变化事件
    event EventHandler<McpServerChangedEventArgs>? ServerChanged;
}
```

### 4. `IMcpServerHealthCheck`

服务器健康检查接口。

```csharp
public interface IMcpServerHealthCheck
{
    Task<McpServerHealthResult> CheckHealthAsync(
        IMcpServerConfiguration configuration,
        CancellationToken cancellationToken = default);
}
```

## 💡 使用场景

### 场景 1: 从配置文件加载

```csharp
public class ConfigurationServerDiscovery : IMcpServerDiscovery
{
    private readonly IConfiguration _configuration;
    
    public async Task<IReadOnlyList<IMcpServerConfiguration>> DiscoverAsync(...)
    {
        // 从 appsettings.json 读取 McpServers 配置
        var configs = _configuration
            .GetSection("McpServers")
            .Get<List<McpServerConfig>>();
        
        return configs.Select(c => new McpServerConfiguration
        {
            Name = c.Name,
            Command = c.Command,
            Arguments = c.Arguments ?? new List<string>(),
            // ...
        }).ToList();
    }
}
```

### 场景 2: 从数据库加载

```csharp
public class DatabaseServerDiscovery : IMcpServerDiscovery
{
    private readonly IDbContext _dbContext;
    
    public async Task<IReadOnlyList<IMcpServerConfiguration>> DiscoverAsync(...)
    {
        // 从数据库读取服务器配置
        var entities = await _dbContext.Servers
            .Where(s => s.Enabled)
            .ToListAsync(cancellationToken);
        
        return entities.Select(e => new McpServerConfiguration
        {
            Id = e.Id.ToString(),
            Name = e.Name,
            Command = e.Command,
            // ...
        }).ToList();
    }
}
```

### 场景 3: 组合多个发现策略

```csharp
public class CompositeServerProvider : IMcpServerProvider
{
    private readonly List<IMcpServerDiscovery> _discoveries;
    
    public async Task<IReadOnlyList<IMcpServerConfiguration>> GetServersAsync(...)
    {
        var allServers = new List<IMcpServerConfiguration>();
        
        // 从所有发现策略收集配置
        foreach (var discovery in _discoveries)
        {
            var servers = await discovery.DiscoverAsync(cancellationToken);
            allServers.AddRange(servers);
        }
        
        // 去重、排序、过滤
        return allServers
            .GroupBy(s => s.Name)
            .Select(g => g.First())
            .ToList();
    }
}
```

### 场景 4: 配置热重载

```csharp
public class HotReloadServerProvider : IMcpServerProvider
{
    private readonly IMcpServerDiscovery _discovery;
    private IReadOnlyList<IMcpServerConfiguration> _cachedServers;
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // 初始加载
        _cachedServers = await _discovery.DiscoverAsync(cancellationToken);
        
        // 监听变化
        if (_discovery.SupportsHotReload)
        {
            _ = _discovery.WatchAsync(async servers =>
            {
                _cachedServers = servers;
                ServerChanged?.Invoke(this, new McpServerChangedEventArgs(
                    McpServerChangeType.Updated, 
                    servers[0]
                ));
            }, cancellationToken);
        }
    }
    
    public event EventHandler<McpServerChangedEventArgs>? ServerChanged;
}
```

## 🔧 扩展点

### 1. 自定义发现策略

实现 `IMcpServerDiscovery` 接口：
- Consul/Etcd 服务发现
- Kubernetes ConfigMap
- Azure App Configuration
- 远程 HTTP API
- Git 仓库

### 2. 装饰器模式增强

```csharp
// 缓存装饰器
public class CachedServerProvider : IMcpServerProvider
{
    private readonly IMcpServerProvider _inner;
    private readonly IMemoryCache _cache;
    
    public async Task<IReadOnlyList<IMcpServerConfiguration>> GetServersAsync(...)
    {
        if (_cache.TryGetValue("servers", out var cached))
            return cached;
            
        var servers = await _inner.GetServersAsync(cancellationToken);
        _cache.Set("servers", servers, TimeSpan.FromMinutes(5));
        return servers;
    }
}
```

### 3. 健康检查实现

```csharp
public class ProcessHealthCheck : IMcpServerHealthCheck
{
    public async Task<McpServerHealthResult> CheckHealthAsync(
        IMcpServerConfiguration configuration,
        CancellationToken cancellationToken)
    {
        try
        {
            // 尝试启动进程并检查
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = configuration.Command,
                Arguments = "--version"
            });
            
            await process.WaitForExitAsync(cancellationToken);
            
            return process.ExitCode == 0
                ? McpServerHealthResult.Healthy()
                : McpServerHealthResult.Unhealthy($"Exit code: {process.ExitCode}");
        }
        catch (Exception ex)
        {
            return McpServerHealthResult.Unhealthy(ex.Message);
        }
    }
}
```

## 🎨 设计模式

### 1. **策略模式 (Strategy Pattern)**
- `IMcpServerDiscovery` - 不同的发现策略
- 运行时切换不同的配置源

### 2. **适配器模式 (Adapter Pattern)**
- 将不同的配置格式适配到统一接口
- 支持多种配置来源

### 3. **组合模式 (Composite Pattern)**
- `CompositeServerProvider` - 组合多个发现策略
- 支持多配置源聚合

### 4. **装饰器模式 (Decorator Pattern)**
- `CachedServerProvider` - 添加缓存能力
- `LoggingServerProvider` - 添加日志记录

### 5. **观察者模式 (Observer Pattern)**
- `ServerChanged` 事件 - 配置变化通知
- 支持响应式编程

## 📊 优势总结

### 架构优势
✅ **职责清晰** - 发现、提供、健康检查各司其职  
✅ **松耦合** - 业务逻辑与配置管理解耦  
✅ **高扩展** - 易于添加新的发现策略  
✅ **可测试** - 接口定义便于模拟测试  

### 功能优势
✅ **多配置源** - 支持配置文件、数据库、服务发现等  
✅ **热重载** - 支持配置动态更新  
✅ **健康检查** - 实时监控服务器状态  
✅ **标签过滤** - 灵活的服务器分组和查询  

### 开发优势
✅ **标准化** - 统一的配置和管理接口  
✅ **复用性** - 配置逻辑可在多个项目间复用  
✅ **维护性** - 清晰的职责划分便于维护  

## 🔗 相关项目

- **McpProxy.Core** - 核心实现
- **McpProxy.Cli** - 命令行工具
- **McpProxy.StdioToSse.WebApi** - Web API 服务
- **McpProxy.SseToStdio.Host** - SSE 到 Stdio 主机

## 📝 版本历史

- **v1.1.0** (2025-12-09)
  - ✅ 移除 `IMcpServerRepository` 接口（过度设计）
  - ✅ 简化 `IMcpServerDiscovery` - 专注于发现
  - ✅ 简化 `IMcpServerProvider` - 专注于提供
  - ✅ 更清晰的职责划分

- **v1.0.0** (2025-12-09)
  - ✅ 初始发布
  - ✅ 定义核心抽象接口
  - ✅ 支持多配置源架构

---

**设计理念**: 简单、灵活、可扩展、职责清晰

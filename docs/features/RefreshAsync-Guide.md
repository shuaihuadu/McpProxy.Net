# RefreshAsync 功能使用指南

## 概述

`RefreshAsync` 功能允许在运行时刷新 MCP 服务器缓存，重新发现服务器并重建工具、提示词和资源的映射关系。这对于以下场景特别有用：

- 配置文件或数据库更改后
- 用户手动触发刷新
- 检测到服务器列表变化

## 接口定义

### IMcpProxyService

```csharp
public interface IMcpProxyService : IAsyncDisposable
{
    // 刷新服务缓存
    Task RefreshAsync(CancellationToken cancellationToken = default);
    
    // 获取服务状态
    ServiceStatusInfo GetStatus();
    
    // 验证服务器健康状态
    Task<HealthCheckResult> ValidateAsync(CancellationToken cancellationToken = default);
}
```

### IMcpRuntime

```csharp
public interface IMcpRuntime : IAsyncDisposable
{
    // 委托给 IMcpProxyService
    Task RefreshAsync(CancellationToken cancellationToken = default);
    ServiceStatusInfo GetStatus();
    Task<HealthCheckResult> ValidateAsync(CancellationToken cancellationToken = default);
}
```

## 使用示例

### 场景 1：MAUI 应用

```csharp
public class McpServersViewModel : ObservableObject
{
    private readonly IMcpProxyService _proxyService;
    
    [RelayCommand]
    public async Task AddServerAsync()
    {
        // 1. 保存到数据库
        await _dbContext.Servers.AddAsync(newServer);
        await _dbContext.SaveChangesAsync();
        
        // 2. 刷新缓存
        await _proxyService.RefreshAsync();
        
        // 3. 更新 UI
        var status = _proxyService.GetStatus();
        ServerCount = status.TotalServers;
        
        await Shell.Current.DisplayAlert(
            "Success", 
            $"Added server. Total: {status.TotalServers}", 
            "OK");
    }
    
    [RelayCommand]
    public async Task RefreshServersAsync()
    {
        try
        {
            IsBusy = true;
            
            // 手动触发刷新
            await _proxyService.RefreshAsync();
            
            // 验证健康状态
            var health = await _proxyService.ValidateAsync();
            
            await Shell.Current.DisplayAlert(
                "Health Check", 
                $"Healthy: {health.HealthyServers}, Unhealthy: {health.UnhealthyServers}", 
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 场景 2：Web API

```csharp
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IMcpRuntime _runtime;
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        await _runtime.RefreshAsync(HttpContext.RequestAborted);
        
        var status = _runtime.GetStatus();
        
        return Ok(new
        {
            message = "Cache refreshed successfully",
            servers = status.TotalServers,
            tools = status.TotalTools,
            prompts = status.TotalPrompts,
            resources = status.TotalResources
        });
    }
    
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = _runtime.GetStatus();
        return Ok(status);
    }
    
    [HttpGet("health")]
    public async Task<IActionResult> CheckHealth()
    {
        var health = await _runtime.ValidateAsync(HttpContext.RequestAborted);
        
        return health.IsHealthy 
            ? Ok(health) 
            : StatusCode(503, health);
    }
}
```

### 场景 3：Console 应用

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var services = BuildServices();
        var proxyService = services.GetRequiredService<IMcpProxyService>();
        
        // 显示初始状态
        var status = proxyService.GetStatus();
        Console.WriteLine($"Initial state:");
        Console.WriteLine($"  Servers: {status.TotalServers}");
        Console.WriteLine($"  Tools: {status.TotalTools}");
        
        // 等待用户输入
        Console.WriteLine("\nPress R to refresh, H for health check, Q to quit");
        
        while (true)
        {
            var key = Console.ReadKey(true).Key;
            
            switch (key)
            {
                case ConsoleKey.R:
                    Console.WriteLine("Refreshing...");
                    await proxyService.RefreshAsync();
                    
                    status = proxyService.GetStatus();
                    Console.WriteLine($"Refreshed! Servers: {status.TotalServers}, Tools: {status.TotalTools}");
                    break;
                    
                case ConsoleKey.H:
                    Console.WriteLine("Checking health...");
                    var health = await proxyService.ValidateAsync();
                    
                    Console.WriteLine($"Health: {(health.IsHealthy ? "Healthy" : "Unhealthy")}");
                    Console.WriteLine($"  Healthy servers: {health.HealthyServers}");
                    Console.WriteLine($"  Unhealthy servers: {health.UnhealthyServers}");
                    break;
                    
                case ConsoleKey.Q:
                    return;
            }
        }
    }
}
```

### 场景 4：配置监听（自动刷新）

```csharp
public class ConfigurationChangeListener : IHostedService
{
    private readonly IMcpProxyService _proxyService;
    private readonly IOptionsMonitor<StdioMcpServersOptions> _optionsMonitor;
    private IDisposable? _changeToken;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 监听配置变化
        _changeToken = _optionsMonitor.OnChange(async (options, name) =>
        {
            _logger.LogInformation("Configuration changed, refreshing MCP proxy service...");
            
            try
            {
                await _proxyService.RefreshAsync(CancellationToken.None);
                _logger.LogInformation("Configuration refresh completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing after configuration change");
            }
        });
        
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _changeToken?.Dispose();
        return Task.CompletedTask;
    }
}

// 注册服务
services.AddHostedService<ConfigurationChangeListener>();
```

## 状态模型

### ServiceStatusInfo

```csharp
public record ServiceStatusInfo
{
    public bool IsInitialized { get; init; }
    public DateTime? LastInitializedAt { get; init; }
    public int TotalServers { get; init; }
    public int TotalTools { get; init; }
    public int TotalPrompts { get; init; }
    public int TotalResources { get; init; }
    public IReadOnlyList<string> ServerNames { get; init; }
}
```

### HealthCheckResult

```csharp
public record HealthCheckResult
{
    public bool IsHealthy { get; init; }
    public int HealthyServers { get; init; }
    public int UnhealthyServers { get; init; }
    public IReadOnlyDictionary<string, ServerHealth> ServerHealths { get; init; }
}

public record ServerHealth
{
    public string ServerName { get; init; }
    public bool IsConnected { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime LastCheckTime { get; init; }
}
```

## 注意事项

1. **线程安全**：`RefreshAsync` 使用 `SemaphoreSlim` 确保线程安全，多个并发调用会依次执行
2. **性能考虑**：刷新操作需要重新连接所有服务器，可能需要几秒钟时间
3. **错误处理**：个别服务器刷新失败不会影响整体操作，只会记录警告日志
4. **最佳实践**：不要频繁调用 `RefreshAsync`，建议使用防抖动或节流机制

## 日志输出示例

```
info: Starting cache refresh operation #1...
debug: Clearing internal caches...
info: Initialization completed. Discovered 3 servers, 15 tools, 5 prompts, 8 resources.
info: Cache refresh #1 completed successfully. Discovered 3 servers, 15 tools, 5 prompts, 8 resources.
```

## 相关文档

- [IMcpProxyService API文档](../api/IMcpProxyService.md)
- [IMcpRuntime API文档](../api/IMcpRuntime.md)
- [配置热更新指南](./configuration-reload.md)

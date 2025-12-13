// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy.Services;

/// <summary>
/// MCP 代理服务初始化后台服务
/// 在应用启动时预先初始化 MCP 服务，避免第一个请求的冷启动延迟
/// </summary>
public class McpProxyInitializationService : IHostedService
{
    private readonly IMcpProxyService _proxyService;
    private readonly ILogger<McpProxyInitializationService> _logger;

    /// <summary>
    /// 初始化 MCP 代理服务初始化后台服务
    /// </summary>
    /// <param name="proxyService">MCP 代理服务实例</param>
    /// <param name="logger">日志记录器</param>
    public McpProxyInitializationService(
        IMcpProxyService proxyService,
        ILogger<McpProxyInitializationService> logger)
    {
        this._proxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 应用启动时触发，执行 MCP 服务的预初始化
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Starting MCP Proxy pre-initialization...");

        DateTime startTime = DateTime.UtcNow;

        try
        {
            // 触发初始化：调用一个简单的操作来触发懒加载
            var status = this._proxyService.GetStatus();

            // 如果尚未初始化，尝试列出工具来触发初始化
            if (!status.IsInitialized)
            {
                this._logger.LogInformation("MCP service not initialized, triggering initialization...");

                _ = await this._proxyService.ListToolsAsync(
                    mcpServerName: null,
                    cursor: null,
                    cancellationToken: cancellationToken);
            }

            // 获取初始化后的状态
            status = this._proxyService.GetStatus();

            TimeSpan elapsed = DateTime.UtcNow - startTime;

            this._logger.LogInformation(
                "MCP Proxy pre-initialization completed in {ElapsedMs}ms. " +
                "Discovered {ServerCount} servers, {ToolCount} tools, {PromptCount} prompts, {ResourceCount} resources.",
                elapsed.TotalMilliseconds,
                status.TotalServers,
                status.TotalTools,
                status.TotalPrompts,
                status.TotalResources);
        }
        catch (Exception ex)
        {
            // 记录错误但不阻止应用启动
            this._logger.LogError(ex, "Error during MCP Proxy pre-initialization. Service will initialize on first request.");
        }
    }

    /// <summary>
    /// 应用停止时触发，清理资源
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("MCP Proxy initialization service stopping...");
        return Task.CompletedTask;
    }
}

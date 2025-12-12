using McpProxy.Core.Services;
using McpProxy.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpProxy.SseToStdio.Host.Workers;

/// <summary>
/// SSE到Stdio代理的后台服务
/// 该服务连接到远程SSE MCP服务器并通过本地Stdio暴露
/// </summary>
public sealed class SseToStdioWorker : BackgroundService
{
    private readonly ISseToStdioService _proxyService;
    private readonly ILogger<SseToStdioWorker> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// 初始化 <see cref="SseToStdioWorker"/> 类的新实例
    /// </summary>
    /// <param name="proxyService">SSE到Stdio代理服务</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="lifetime">应用程序生命周期</param>
    /// <exception cref="ArgumentNullException">当任何参数为 null 时抛出</exception>
    public SseToStdioWorker(
        ISseToStdioService proxyService,
        ILogger<SseToStdioWorker> logger,
        IHostApplicationLifetime lifetime)
    {
        this._proxyService = proxyService ?? throw new ArgumentNullException(nameof(proxyService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    /// <summary>
    /// 执行后台服务的主要逻辑
    /// </summary>
    /// <param name="stoppingToken">停止令牌</param>
    /// <returns>表示异步操作的任务</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("SSE to Stdio proxy service is starting...");

        try
        {
            // 等待应用启动完成
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);

            this._logger.LogInformation("SSE to Stdio proxy service is running");

            // 运行代理服务
            await this._proxyService.RunAsync(stoppingToken).ConfigureAwait(false);

            this._logger.LogInformation("SSE to Stdio proxy service completed successfully");
        }
        catch (OperationCanceledException)
        {
            // 正常停止，不记录为错误
            this._logger.LogInformation("SSE to Stdio proxy service is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            // 记录致命错误
            this._logger.LogCritical(ex, "SSE to Stdio proxy service crashed unexpectedly");

            // 请求应用程序停止
            this._lifetime.StopApplication();
            
            throw;
        }
    }

    /// <summary>
    /// 停止后台服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("SSE to Stdio proxy service is stopping...");

        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation("SSE to Stdio proxy service has stopped");
    }
}

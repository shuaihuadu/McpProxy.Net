namespace McpProxy.Abstractions.Services;

/// <summary>
/// MCP代理服务的接口定义
/// </summary>
public interface IProxyService
{
    /// <summary>
    /// 异步运行代理服务
    /// </summary>
    /// <param name="cancellationToken">用于取消操作的取消令牌</param>
    /// <returns>表示异步运行操作的任务</returns>
    Task RunAsync(CancellationToken cancellationToken = default);
}

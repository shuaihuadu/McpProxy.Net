using ModelContextProtocol.Protocol;
using McpProxy.Abstractions.Models;

namespace McpProxy.Abstractions.Services;

/// <summary>
/// Stdio到SSE/HTTP代理服务接口
/// 用于将本地Stdio MCP服务器转换为HTTP/SSE端点
/// </summary>
public interface IStdioToSseService
{
    /// <summary>
    /// 初始化并连接到所有配置的Stdio服务器
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用的工具
    /// </summary>
    /// <param name="serverFilter">服务器过滤器（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<ListToolsResult> ListToolsAsync(string? serverFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 调用指定的工具
    /// </summary>
    /// <param name="toolName">工具名称（可能包含服务器前缀）</param>
    /// <param name="arguments">工具参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<CallToolResult> CallToolAsync(string toolName, object? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用的提示
    /// </summary>
    /// <param name="serverFilter">服务器过滤器（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<ListPromptsResult> ListPromptsAsync(string? serverFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定的提示
    /// </summary>
    /// <param name="promptName">提示名称（可能包含服务器前缀）</param>
    /// <param name="arguments">提示参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<GetPromptResult> GetPromptAsync(string promptName, object? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出所有可用的资源
    /// </summary>
    /// <param name="serverFilter">服务器过滤器（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<ListResourcesResult> ListResourcesAsync(string? serverFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取指定的资源
    /// </summary>
    /// <param name="resourceUri">资源URI（可能包含服务器前缀）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<ReadResourceResult> ReadResourceAsync(string resourceUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有服务器的状态信息
    /// </summary>
    Task<IReadOnlyList<ServerStatusInfo>> GetServerStatusAsync();

    /// <summary>
    /// 获取聚合的服务器能力
    /// </summary>
    ServerCapabilities GetAggregatedCapabilities();
}

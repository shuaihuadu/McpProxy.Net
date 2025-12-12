// Copyright (c) IdeaTech. All rights reserved.

namespace McpProxy.Abstractions;

/// <summary>
/// MCP 服务器发现策略接口
/// 负责从特定来源发现和加载 MCP 服务器配置
/// </summary>
public interface IMcpServerDiscovery
{
    /// <summary>
    /// 获取发现策略的名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 发现 MCP 服务器配置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发现的服务器配置列表</returns>
    Task<IReadOnlyList<IMcpServerConfiguration>> DiscoverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 支持配置热重载（可选）
    /// </summary>
    bool SupportsHotReload { get; }

    /// <summary>
    /// 监听配置变化（如果支持热重载）
    /// </summary>
    /// <param name="callback">变化回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示监听任务</returns>
    Task WatchAsync(
        Func<IReadOnlyList<IMcpServerConfiguration>, Task> callback,
        CancellationToken cancellationToken = default);
}

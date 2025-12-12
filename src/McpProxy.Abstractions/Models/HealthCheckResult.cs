// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy;

/// <summary>
/// 健康检查结果
/// 包含所有服务器的健康状态信息
/// </summary>
public record HealthCheckResult
{
    /// <summary>
    /// 获取整体健康状态
    /// 当所有服务器都健康时为 true
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// 获取健康的服务器数量
    /// </summary>
    public int HealthyServers { get; init; }

    /// <summary>
    /// 获取不健康的服务器数量
    /// </summary>
    public int UnhealthyServers { get; init; }

    /// <summary>
    /// 获取所有服务器的健康状态详情
    /// 键为服务器名称，值为健康状态详情
    /// </summary>
    public IReadOnlyDictionary<string, ServerHealth> ServerHealths { get; init; }
        = new Dictionary<string, ServerHealth>();
}

/// <summary>
/// 单个服务器的健康状态
/// </summary>
public record ServerHealth
{
    /// <summary>
    /// 获取服务器名称
    /// </summary>
    public string ServerName { get; init; } = string.Empty;

    /// <summary>
    /// 获取服务器是否已连接
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// 获取错误消息
    /// 如果服务器健康则为 null
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 获取最后一次检查的时间
    /// </summary>
    public DateTime LastCheckTime { get; init; }
}

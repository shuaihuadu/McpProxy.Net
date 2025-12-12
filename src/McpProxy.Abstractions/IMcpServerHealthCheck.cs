// Copyright (c) IdeaTech. All rights reserved.

namespace McpProxy.Abstractions;

/// <summary>
/// MCP 服务器健康检查接口
/// 用于检查服务器的健康状态
/// </summary>
public interface IMcpServerHealthCheck
{
    /// <summary>
    /// 检查服务器健康状态
    /// </summary>
    /// <param name="configuration">服务器配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>健康检查结果</returns>
    Task<McpServerHealthResult> CheckHealthAsync(
        IMcpServerConfiguration configuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// MCP 服务器健康状态
/// </summary>
public enum McpServerHealthStatus
{
    /// <summary>
    /// 健康
    /// </summary>
    Healthy,

    /// <summary>
    /// 降级（部分功能不可用）
    /// </summary>
    Degraded,

    /// <summary>
    /// 不健康
    /// </summary>
    Unhealthy,

    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown
}

/// <summary>
/// MCP 服务器健康检查结果
/// </summary>
public class McpServerHealthResult
{
    /// <summary>
    /// 获取或设置健康状态
    /// </summary>
    public McpServerHealthStatus Status { get; init; }

    /// <summary>
    /// 获取或设置描述信息
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 获取或设置检查时间
    /// </summary>
    public DateTime CheckTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 获取或设置响应时间（毫秒）
    /// </summary>
    public long? ResponseTimeMs { get; init; }

    /// <summary>
    /// 获取或设置附加数据
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    /// <summary>
    /// 创建健康结果
    /// </summary>
    public static McpServerHealthResult Healthy(string? description = null) =>
        new() { Status = McpServerHealthStatus.Healthy, Description = description };

    /// <summary>
    /// 创建降级结果
    /// </summary>
    public static McpServerHealthResult Degraded(string? description = null) =>
        new() { Status = McpServerHealthStatus.Degraded, Description = description };

    /// <summary>
    /// 创建不健康结果
    /// </summary>
    public static McpServerHealthResult Unhealthy(string? description = null) =>
        new() { Status = McpServerHealthStatus.Unhealthy, Description = description };

    /// <summary>
    /// 创建未知状态结果
    /// </summary>
    public static McpServerHealthResult Unknown(string? description = null) =>
        new() { Status = McpServerHealthStatus.Unknown, Description = description };
}

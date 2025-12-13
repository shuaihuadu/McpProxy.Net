// Copyright (c) ShuaiHua Du. All rights reserved.

namespace McpProxy;

/// <summary>
/// 服务状态信息
/// 包含服务的初始化状态、服务器和资源统计信息
/// </summary>
public record ServiceStatus
{
    /// <summary>
    /// 获取服务是否已初始化
    /// </summary>
    public bool IsInitialized { get; init; }

    /// <summary>
    /// 获取最后一次初始化的时间
    /// 如果从未初始化则为 null
    /// </summary>
    public DateTime? LastInitializedAt { get; init; }

    /// <summary>
    /// 获取已发现的服务器总数
    /// </summary>
    public int TotalServers { get; init; }

    /// <summary>
    /// 获取可用的工具总数
    /// </summary>
    public int TotalTools { get; init; }

    /// <summary>
    /// 获取可用的提示词总数
    /// </summary>
    public int TotalPrompts { get; init; }

    /// <summary>
    /// 获取可用的资源总数
    /// </summary>
    public int TotalResources { get; init; }

    /// <summary>
    /// 获取所有服务器名称的列表
    /// </summary>
    public IReadOnlyList<string> ServerNames { get; init; } = [];
}

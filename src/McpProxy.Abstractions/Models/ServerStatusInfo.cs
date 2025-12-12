// Copyright (c) IdeaTech. All rights reserved.

using ModelContextProtocol.Protocol;

namespace McpProxy.Abstractions.Models;

/// <summary>
/// 服务器状态信息
/// </summary>
public sealed class ServerStatusInfo
{
    /// <summary>
    /// 获取服务器名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 获取是否已连接
    /// </summary>
    public required bool IsConnected { get; init; }

    /// <summary>
    /// 获取服务器报告的名称
    /// </summary>
    public string? ServerName { get; init; }

    /// <summary>
    /// 获取服务器版本
    /// </summary>
    public string? ServerVersion { get; init; }

    /// <summary>
    /// 获取最后心跳时间
    /// </summary>
    public DateTime? LastHeartbeat { get; init; }

    /// <summary>
    /// 获取服务器能力
    /// </summary>
    public ServerCapabilities? Capabilities { get; init; }
}
